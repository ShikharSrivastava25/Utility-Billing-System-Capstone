using AutoMapper;
using Microsoft.EntityFrameworkCore;
using UtilityBillingSystem.Data;
using UtilityBillingSystem.Models.Core;
using UtilityBillingSystem.Models.Dto.Tariff;
using UtilityBillingSystem.Services.Interfaces;
using UtilityBillingSystem.Services.Helpers;

namespace UtilityBillingSystem.Services
{
    public class TariffService : ITariffService
    {
        private readonly AppDbContext _context;
        private readonly IAuditLogService _auditLogService;
        private readonly IMapper _mapper;

        public TariffService(AppDbContext context, IAuditLogService auditLogService, IMapper mapper)
        {
            _context = context;
            _auditLogService = auditLogService;
            _mapper = mapper;
        }

        public async Task<IEnumerable<TariffDto>> GetTariffsAsync()
        {
            var tariffs = await _context.Tariffs
                .OrderBy(t => t.Name.ToLower())
                .ToListAsync();
            return _mapper.Map<IEnumerable<TariffDto>>(tariffs);
        }

        public async Task<TariffDto> GetTariffByIdAsync(string id)
        {
            var tariff = await _context.Tariffs.FindAsync(id);
            if (tariff == null)
                throw new KeyNotFoundException("Tariff not found");

            return _mapper.Map<TariffDto>(tariff);
        }

        public async Task<TariffDto> CreateTariffAsync(TariffDto dto, string currentUserEmail)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new ArgumentException("Name is required");

            if (string.IsNullOrWhiteSpace(dto.UtilityTypeId))
                throw new ArgumentException("Utility type is required");

            // Validate that utility type exists
            var utilityTypeExists = await _context.UtilityTypes.AnyAsync(u => u.Id == dto.UtilityTypeId);
            if (!utilityTypeExists)
                throw new KeyNotFoundException("Utility type not found");

            var tariff = new Tariff
            {
                Name = dto.Name,
                UtilityTypeId = dto.UtilityTypeId,
                BaseRate = dto.BaseRate,
                FixedCharge = dto.FixedCharge,
                TaxPercentage = dto.TaxPercentage,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.Tariffs.Add(tariff);
            await _context.SaveChangesAsync();

            await _auditLogService.LogActionAsync("TARIFF_CREATE", $"Created new tariff plan '{tariff.Name}'.", currentUserEmail);

            return _mapper.Map<TariffDto>(tariff);
        }

        public async Task<TariffDto> UpdateTariffAsync(string id, TariffDto dto, string currentUserEmail)
        {
            var tariff = await _context.Tariffs
                .Include(t => t.Connections)
                .FirstOrDefaultAsync(t => t.Id == id);
            if (tariff == null)
                throw new KeyNotFoundException("Tariff not found");

            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new ArgumentException("Name is required");

            if (string.IsNullOrWhiteSpace(dto.UtilityTypeId))
                throw new ArgumentException("Utility type is required");

            // Validate that utility type exists and is enabled
            var utilityType = await _context.UtilityTypes.FindAsync(dto.UtilityTypeId);
            if (utilityType == null)
                throw new KeyNotFoundException("Utility type not found");
            
            if (utilityType.Status != "Enabled")
                throw new InvalidOperationException("Cannot update tariff for a disabled utility type. Please enable the utility type first.");

            if (dto.UtilityTypeId != tariff.UtilityTypeId && tariff.Connections.Any())
                throw new InvalidOperationException("Cannot change utility type for a tariff that has existing connections. Please remove or update all connections first.");

            // Check if all meter readings have been taken and billed for the current billing period
            var activeConnections = tariff.Connections
                .Where(c => c.Status == "Active")
                .ToList();

            if (activeConnections.Any())
            {
                var now = DateTime.UtcNow;
                var connectionIds = activeConnections.Select(c => c.Id).ToList();

                // Get all connections with their utility types, billing cycles, users, and tariffs
                var connectionsWithDetails = await _context.Connections
                    .Where(c => connectionIds.Contains(c.Id))
                    .Include(c => c.User)
                    .Include(c => c.UtilityType)
                        .ThenInclude(ut => ut!.BillingCycle)
                    .Include(c => c.Tariff)
                    .ToListAsync();

                var issues = new List<string>();

                foreach (var connection in connectionsWithDetails)
                {
                    if (connection.User == null || connection.User.Status != "Active")
                        continue;

                    if (connection.UtilityType == null || connection.UtilityType.Status != "Enabled")
                        continue;

                    if (connection.UtilityType.BillingCycle == null || !connection.UtilityType.BillingCycle.IsActive)
                        continue;

                    if (connection.Tariff == null || !connection.Tariff.IsActive)
                        continue;

                    var billingCycle = connection.UtilityType.BillingCycle;
                    var (currentPeriodStart, currentPeriodEnd) = BillingCycleHelper.GetCurrentBillingPeriod(billingCycle, now);

                    var hasReading = await _context.MeterReadings
                        .AnyAsync(mr => mr.ConnectionId == connection.Id &&
                                       mr.BillingCycleId == billingCycle.Id &&
                                       mr.ReadingDate >= currentPeriodStart &&
                                       mr.ReadingDate <= currentPeriodEnd);

                    if (!hasReading)
                    {
                        issues.Add($"{connection.MeterNumber} (no reading taken)");
                        continue;
                    }

                    // Check if reading is unbilled (status != "Billed")
                    // This ensures all readings are billed before allowing tariff update
                    var hasUnbilledReading = await _context.MeterReadings
                        .AnyAsync(mr => mr.ConnectionId == connection.Id &&
                                       mr.Status != "Billed" &&
                                       mr.BillingCycleId == billingCycle.Id &&
                                       mr.ReadingDate >= currentPeriodStart &&
                                       mr.ReadingDate <= currentPeriodEnd);

                    if (hasUnbilledReading)
                    {
                        issues.Add($"{connection.MeterNumber} (unbilled reading)");
                    }
                }

                if (issues.Any())
                {
                    throw new InvalidOperationException(
                        $"Cannot update tariff plan. Please ensure all meter readings are taken and all bills are generated first. " +
                        $"Found {issues.Count} issue(s): {string.Join(", ", issues.Take(5))}" +
                        (issues.Count > 5 ? " and more..." : "."));
                }
            }

            tariff.Name = dto.Name;
            tariff.UtilityTypeId = dto.UtilityTypeId;
            tariff.BaseRate = dto.BaseRate;
            tariff.FixedCharge = dto.FixedCharge;
            tariff.TaxPercentage = dto.TaxPercentage;

            await _context.SaveChangesAsync();

            await _auditLogService.LogActionAsync("TARIFF_UPDATE", 
                $"Updated tariff plan '{tariff.Name}'. Rates: Base={tariff.BaseRate}, Fixed={tariff.FixedCharge}, Tax={tariff.TaxPercentage}%.", 
                currentUserEmail);

            return _mapper.Map<TariffDto>(tariff);
        }

        public async Task DeleteTariffAsync(string id, string currentUserEmail)
        {
            var tariff = await _context.Tariffs
                .Include(t => t.Connections)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tariff == null)
                throw new KeyNotFoundException("Tariff not found");

            // Check if tariff has any connections (active or inactive)
            if (tariff.Connections.Any())
                throw new InvalidOperationException("Cannot delete tariff with existing connections. Please update or remove all connections first.");

            var tariffName = tariff.Name;
            _context.Tariffs.Remove(tariff);
            await _context.SaveChangesAsync();

            await _auditLogService.LogActionAsync("TARIFF_DELETE", $"Deleted tariff '{tariffName}'.", currentUserEmail);
        }
    }
}

