using AutoMapper;
using Microsoft.EntityFrameworkCore;
using UtilityBillingSystem.Data;
using UtilityBillingSystem.Models.Core;
using UtilityBillingSystem.Models.Dto.BillingCycle;
using UtilityBillingSystem.Services.Interfaces;
using UtilityBillingSystem.Services.Helpers;

namespace UtilityBillingSystem.Services
{
    public class BillingCycleService : IBillingCycleService
    {
        private readonly AppDbContext _context;
        private readonly IAuditLogService _auditLogService;
        private readonly IMapper _mapper;

        public BillingCycleService(AppDbContext context, IAuditLogService auditLogService, IMapper mapper)
        {
            _context = context;
            _auditLogService = auditLogService;
            _mapper = mapper;
        }

        public async Task<IEnumerable<BillingCycleDto>> GetBillingCyclesAsync()
        {
            var cycles = await _context.BillingCycles.ToListAsync();
            return _mapper.Map<IEnumerable<BillingCycleDto>>(cycles);
        }

        public async Task<BillingCycleDto> GetBillingCycleByIdAsync(string id)
        {
            var cycle = await _context.BillingCycles.FindAsync(id);
            if (cycle == null)
                throw new KeyNotFoundException("Billing cycle not found");

            return _mapper.Map<BillingCycleDto>(cycle);
        }

        public async Task<BillingCycleDto> CreateBillingCycleAsync(BillingCycleDto dto, string currentUserEmail)
        {
            var cycle = new BillingCycle
            {
                Name = dto.Name,
                GenerationDay = dto.GenerationDay,
                DueDateOffset = dto.DueDateOffset,
                GracePeriod = dto.GracePeriod,
                IsActive = dto.IsActive
            };

            _context.BillingCycles.Add(cycle);
            await _context.SaveChangesAsync();

            await _auditLogService.LogActionAsync("BILLING_CYCLE_CREATE", $"Created new billing cycle '{cycle.Name}'.", currentUserEmail);

            return _mapper.Map<BillingCycleDto>(cycle);
        }

        public async Task<BillingCycleDto> UpdateBillingCycleAsync(string id, BillingCycleDto dto, string currentUserEmail)
        {
            var cycle = await _context.BillingCycles
                .Include(bc => bc.UtilityTypes)
                .FirstOrDefaultAsync(bc => bc.Id == id);
            
            if (cycle == null)
                throw new KeyNotFoundException("Billing cycle not found");

            if (dto.GenerationDay < 1 || dto.GenerationDay > 28)
                throw new ArgumentException("Generation day must be between 1 and 28");

            // Validate due date offset (should be positive)
            if (dto.DueDateOffset < 0)
                throw new ArgumentException("Due date offset must be 0 or greater");

            // Validate grace period (should be non-negative)
            if (dto.GracePeriod < 0)
                throw new ArgumentException("Grace period must be 0 or greater");

            // Check if trying to deactivate billing cycle that is assigned to utility types
            if (!dto.IsActive && cycle.UtilityTypes.Any())
                throw new InvalidOperationException("Cannot deactivate billing cycle that is assigned to utility types. Please remove assignments first.");

            // Check if any billing cycle properties are being changed (that would affect active periods)
            bool isChangingCriticalProperties = 
                cycle.GenerationDay != dto.GenerationDay ||
                cycle.DueDateOffset != dto.DueDateOffset ||
                cycle.GracePeriod != dto.GracePeriod;

            // If critical properties are being changed, check for active meter readings
            if (isChangingCriticalProperties)
            {
                var now = DateTime.UtcNow;
                var (currentPeriodStart, currentPeriodEnd) = BillingCycleHelper.GetCurrentBillingPeriod(cycle, now);
                var (previousPeriodStart, previousPeriodEnd) = BillingCycleHelper.GetCurrentBillingPeriod(cycle, now.AddMonths(-1));

                // Check for active meter readings in current or previous billing period
                var hasActiveReadings = await _context.MeterReadings
                    .AnyAsync(mr => mr.BillingCycleId == id &&
                                    mr.Status == "ReadyForBilling" &&
                                    ((mr.ReadingDate >= currentPeriodStart && mr.ReadingDate <= currentPeriodEnd) ||
                                     (mr.ReadingDate >= previousPeriodStart && mr.ReadingDate <= previousPeriodEnd)));

                if (hasActiveReadings)
                {
                    throw new InvalidOperationException(
                        "Cannot modify billing cycle properties (Generation Day, Due Date Offset, or Grace Period) " +
                        "while there are active meter readings in the current or previous billing period. " +
                        "Please wait until all readings are billed, or deactivate the billing cycle first.");
                }

                // Also check if there are connections that need readings for the current billing period
                var utilityTypeIds = cycle.UtilityTypes.Select(ut => ut.Id).ToList();
                
                if (utilityTypeIds.Any())
                {
                    // Find active connections for these utility types that are eligible for readings
                    var eligibleConnectionIds = await _context.Connections
                        .Where(c => c.Status == "Active" && 
                                   c.User.Status == "Active" &&
                                   utilityTypeIds.Contains(c.UtilityTypeId) &&
                                   c.UtilityType != null &&
                                   c.UtilityType.Status == "Enabled" &&
                                   c.UtilityType.BillingCycleId == id &&
                                   c.Tariff != null &&
                                   c.Tariff.IsActive)
                        .Select(c => c.Id)
                        .ToListAsync();

                    if (eligibleConnectionIds.Any())
                    {
                        var connectionsWithReadings = await _context.MeterReadings
                            .Where(mr => eligibleConnectionIds.Contains(mr.ConnectionId) &&
                                        mr.BillingCycleId == id &&
                                        mr.ReadingDate >= currentPeriodStart &&
                                        mr.ReadingDate <= currentPeriodEnd)
                            .Select(mr => mr.ConnectionId)
                            .Distinct()
                            .ToListAsync();

                        // If any connection doesn't have a reading, block the update
                        var connectionsNeedingReadings = eligibleConnectionIds.Except(connectionsWithReadings).ToList();
                        
                        if (connectionsNeedingReadings.Any())
                        {
                            throw new InvalidOperationException(
                                "Cannot modify billing cycle properties (Generation Day, Due Date Offset, or Grace Period) " +
                                "while there are connections that need meter readings for the current billing period. " +
                                "Please wait until all meter readings are taken, or deactivate the billing cycle first.");
                        }
                    }
                }
            }

            cycle.Name = dto.Name;
            cycle.GenerationDay = dto.GenerationDay;
            cycle.DueDateOffset = dto.DueDateOffset;
            cycle.GracePeriod = dto.GracePeriod;
            cycle.IsActive = dto.IsActive;

            await _context.SaveChangesAsync();

            await _auditLogService.LogActionAsync("BILLING_CYCLE_UPDATE", $"Updated billing cycle '{cycle.Name}'.", currentUserEmail);

            return _mapper.Map<BillingCycleDto>(cycle);
        }

        public async Task DeleteBillingCycleAsync(string id, string currentUserEmail)
        {
            var cycle = await _context.BillingCycles
                .Include(bc => bc.UtilityTypes)
                .FirstOrDefaultAsync(bc => bc.Id == id);

            if (cycle == null)
                throw new KeyNotFoundException("Billing cycle not found");

            // Check if billing cycle is assigned to any utility types
            if (cycle.UtilityTypes.Any())
                throw new InvalidOperationException("Cannot delete billing cycle that is assigned to utility types. Please remove assignments first.");

            // Check if billing cycle has any meter readings
            var hasMeterReadings = await _context.MeterReadings.AnyAsync(mr => mr.BillingCycleId == id);
            if (hasMeterReadings)
                throw new InvalidOperationException("Cannot delete billing cycle with existing meter readings. Please handle meter readings first.");

            var cycleName = cycle.Name;
            _context.BillingCycles.Remove(cycle);
            await _context.SaveChangesAsync();

            await _auditLogService.LogActionAsync("BILLING_CYCLE_DELETE", $"Deleted billing cycle '{cycleName}'.", currentUserEmail);
        }
    }
}

