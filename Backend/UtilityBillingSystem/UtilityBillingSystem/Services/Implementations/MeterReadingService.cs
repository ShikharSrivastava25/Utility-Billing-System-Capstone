using AutoMapper;
using Microsoft.EntityFrameworkCore;
using UtilityBillingSystem.Data;
using UtilityBillingSystem.Models.Core;
using UtilityBillingSystem.Models.Dto.MeterReading;
using UtilityBillingSystem.Services.Interfaces;
using UtilityBillingSystem.Services.Helpers;

namespace UtilityBillingSystem.Services
{
    public class MeterReadingService : IMeterReadingService
    {
        private readonly AppDbContext _context;
        private readonly IAuditLogService _auditLogService;
        private readonly IMapper _mapper;

        public MeterReadingService(AppDbContext context, IAuditLogService auditLogService, IMapper mapper)
        {
            _context = context;
            _auditLogService = auditLogService;
            _mapper = mapper;
        }

        public async Task<IEnumerable<ConnectionForReadingDto>> GetConnectionsNeedingReadingsAsync()
        {
            var now = DateTime.UtcNow;
            var currentMonth = now.Month;
            var currentYear = now.Year;

            // Get all active connections with active (non-deleted) users and their utility types and billing cycles
            var allConnections = await _context.Connections
                .Where(c => c.Status == "Active" && c.User.Status == "Active")
                .Include(c => c.User)
                .Include(c => c.UtilityType)
                    .ThenInclude(ut => ut!.BillingCycle)
                .Include(c => c.Tariff)
                .ToListAsync();

            var connectionsNeedingReadings = new List<ConnectionForReadingDto>();

            foreach (var connection in allConnections)
            {
                if (connection.UtilityType == null)
                    continue;

                if (connection.UtilityType.Status != "Enabled")
                    continue;

                if (connection.UtilityType.BillingCycle == null)
                    continue;

                var billingCycle = connection.UtilityType.BillingCycle;

                if (!billingCycle.IsActive)
                    continue;

                if (connection.Tariff == null)
                    continue;

                if (!connection.Tariff.IsActive)
                    continue; // Skip inactive tariffs

                // Determine current billing period
                var (periodStart, periodEnd) = BillingCycleHelper.GetCurrentBillingPeriod(billingCycle, now);

                // Check if reading already exists for this connection and billing cycle
                var existingReading = await _context.MeterReadings
                    .Where(mr => mr.ConnectionId == connection.Id && 
                                 mr.BillingCycleId == billingCycle.Id &&
                                 mr.ReadingDate >= periodStart && 
                                 mr.ReadingDate <= periodEnd)
                    .FirstOrDefaultAsync();

                // Only include if no reading exists for current period
                if (existingReading == null)
                {
                    var previousReading = await _context.MeterReadings
                        .Where(mr => mr.ConnectionId == connection.Id && mr.Status == "Billed")
                        .OrderByDescending(mr => mr.ReadingDate)
                        .Select(mr => mr.CurrentReading)
                        .FirstOrDefaultAsync();

                    connectionsNeedingReadings.Add(new ConnectionForReadingDto
                    {
                        Id = connection.Id,
                        UserId = connection.UserId,
                        ConsumerName = connection.User.FullName,
                        UtilityTypeId = connection.UtilityTypeId,
                        UtilityName = connection.UtilityType.Name,
                        TariffId = connection.TariffId,
                        MeterNumber = connection.MeterNumber,
                        PreviousReading = previousReading,
                        BillingCycleId = billingCycle.Id,
                        BillingCycleName = billingCycle.Name
                    });
                }
            }

            return connectionsNeedingReadings;
        }

        public async Task<decimal?> GetPreviousReadingAsync(string connectionId)
        {
            var lastReading = await _context.MeterReadings
                .Where(mr => mr.ConnectionId == connectionId)
                .OrderByDescending(mr => mr.ReadingDate)
                .FirstOrDefaultAsync();

            return lastReading?.CurrentReading;
        }

        public async Task<MeterReadingResponseDto> CreateMeterReadingAsync(MeterReadingRequestDto dto, string userEmail)
        {
            var connection = await _context.Connections
                .Include(c => c.User)
                .Include(c => c.UtilityType)
                    .ThenInclude(ut => ut!.BillingCycle)
                .Include(c => c.Tariff)
                .FirstOrDefaultAsync(c => c.Id == dto.ConnectionId);

            if (connection == null)
                throw new KeyNotFoundException("Connection not found");

            if (connection.Status != "Active")
                throw new InvalidOperationException("Connection is not active");

            if (connection.User.Status != "Active")
                throw new InvalidOperationException("Cannot create meter reading for an inactive or deleted consumer");

            if (connection.UtilityType == null)
                throw new InvalidOperationException("Utility type not found");

            if (connection.UtilityType.Status != "Enabled")
                throw new InvalidOperationException("Cannot create meter reading for a disabled utility type. Please enable the utility type first.");

            if (connection.UtilityType.BillingCycle == null)
                throw new InvalidOperationException("Utility type does not have a billing cycle configured");

            var billingCycle = connection.UtilityType.BillingCycle;

            if (!billingCycle.IsActive)
                throw new InvalidOperationException("Cannot create meter reading for a utility with an inactive billing cycle. Please activate the billing cycle first.");

            // Check if tariff is active
            if (connection.Tariff == null)
                throw new InvalidOperationException("Connection does not have a tariff assigned");

            if (!connection.Tariff.IsActive)
                throw new InvalidOperationException("Cannot create meter reading for a connection with an inactive tariff. Please activate the tariff first.");

            // Get previous reading
            var previousReading = await GetPreviousReadingAsync(dto.ConnectionId) ?? 0;

            // Validate current reading >= previous reading
            if (dto.CurrentReading < previousReading)
                throw new InvalidOperationException($"Current reading ({dto.CurrentReading}) must be greater than or equal to previous reading ({previousReading})");

            // Determine current billing period and calculate generation day
            var now = DateTime.UtcNow;
            var (periodStart, periodEnd) = BillingCycleHelper.GetCurrentBillingPeriod(billingCycle, now);
            
            // Check if reading already exists for this billing cycle
            var existingReading = await _context.MeterReadings
                .Where(mr => mr.ConnectionId == dto.ConnectionId &&
                             mr.BillingCycleId == billingCycle.Id &&
                             mr.ReadingDate >= periodStart &&
                             mr.ReadingDate <= periodEnd)
                .FirstOrDefaultAsync();

            if (existingReading != null)
                throw new InvalidOperationException("A reading already exists for this connection in the current billing period");

            // Calculate consumption
            var consumption = dto.CurrentReading - previousReading;

            var periodYear = periodStart.Year;
            var periodMonth = periodStart.Month;
            var generationDay = Math.Min(billingCycle.GenerationDay, DateTime.DaysInMonth(periodYear, periodMonth));
            var readingDateForBilling = new DateTime(periodYear, periodMonth, generationDay, 0, 0, 0, DateTimeKind.Utc);

            // Create meter reading
            var meterReading = new MeterReading
            {
                ConnectionId = dto.ConnectionId,
                PreviousReading = previousReading,
                CurrentReading = dto.CurrentReading,
                Consumption = consumption,
                ReadingDate = readingDateForBilling,
                Status = "ReadyForBilling",
                RecordedBy = userEmail,
                BillingCycleId = billingCycle.Id,
                TariffId = connection.TariffId,
                CreatedAt = DateTime.UtcNow
            };

            _context.MeterReadings.Add(meterReading);
            await _context.SaveChangesAsync();

            await _auditLogService.LogActionAsync(
                "METER_READING_CREATE",
                $"Created meter reading for connection {connection.MeterNumber}. Consumption: {consumption} units.",
                userEmail);

            return await GetMeterReadingByIdAsync(meterReading.Id);
        }

        public async Task<IEnumerable<MeterReadingResponseDto>> GetMeterReadingHistoryAsync(
            DateTime? startDate = null,
            DateTime? endDate = null,
            string? utilityTypeId = null,
            string? consumerName = null,
            string? status = null,
            int page = 1,
            int pageSize = 50)
        {
            var query = _context.MeterReadings
                .Include(mr => mr.Connection)
                    .ThenInclude(c => c.User)
                .Include(mr => mr.Connection)
                    .ThenInclude(c => c.UtilityType)
                .Include(mr => mr.Tariff)
                .AsQueryable();

            if (startDate.HasValue)
                query = query.Where(mr => mr.ReadingDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(mr => mr.ReadingDate <= endDate.Value);

            if (!string.IsNullOrEmpty(utilityTypeId))
                query = query.Where(mr => mr.Connection.UtilityTypeId == utilityTypeId);

            if (!string.IsNullOrEmpty(consumerName))
                query = query.Where(mr => mr.Connection.User.FullName.Contains(consumerName));

            if (!string.IsNullOrEmpty(status))
                query = query.Where(mr => mr.Status == status);

            var skip = (page - 1) * pageSize;
            var readings = await query
                .OrderBy(mr => mr.Status == "ReadyForBilling" ? 0 : 1)
                .ThenByDescending(mr => mr.ReadingDate)
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();

            return _mapper.Map<IEnumerable<MeterReadingResponseDto>>(readings);
        }

        public async Task<MeterReadingResponseDto?> GetMeterReadingByIdAsync(string id)
        {
            var reading = await _context.MeterReadings
                .Include(mr => mr.Connection)
                    .ThenInclude(c => c.User)
                .Include(mr => mr.Connection)
                    .ThenInclude(c => c.UtilityType)
                .Include(mr => mr.Tariff)
                .FirstOrDefaultAsync(mr => mr.Id == id);

            if (reading == null)
                return null;

            return _mapper.Map<MeterReadingResponseDto>(reading);
        }

        public async Task<MeterReadingResponseDto> UpdateMeterReadingAsync(string id, decimal currentReading, string userEmail)
        {
            var reading = await _context.MeterReadings
                .Include(mr => mr.Connection)
                .FirstOrDefaultAsync(mr => mr.Id == id);

            if (reading == null)
                throw new KeyNotFoundException("Meter reading not found");

            if (reading.Status == "Billed")
                throw new InvalidOperationException("Cannot edit a reading that has already been billed");

            // Validate current reading >= previous reading
            if (currentReading < reading.PreviousReading)
                throw new InvalidOperationException($"Current reading ({currentReading}) must be greater than or equal to previous reading ({reading.PreviousReading})");

            reading.CurrentReading = currentReading;
            reading.Consumption = currentReading - reading.PreviousReading;
            reading.RecordedBy = userEmail;

            await _context.SaveChangesAsync();

            await _auditLogService.LogActionAsync(
                "METER_READING_UPDATE",
                $"Updated meter reading for connection {reading.Connection.MeterNumber}. New consumption: {reading.Consumption} units.",
                userEmail);

            return await GetMeterReadingByIdAsync(id);
        }
    }
}

