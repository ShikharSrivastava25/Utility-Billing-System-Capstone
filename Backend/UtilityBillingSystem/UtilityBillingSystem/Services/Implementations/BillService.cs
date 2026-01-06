using Microsoft.EntityFrameworkCore;
using UtilityBillingSystem.Data;
using UtilityBillingSystem.Models.Core;
using UtilityBillingSystem.Models.Dto.Bill;
using UtilityBillingSystem.Models.Core.Notification;
using UtilityBillingSystem.Services.Interfaces;
using UtilityBillingSystem.Services.Helpers;

namespace UtilityBillingSystem.Services
{
    public class BillService : IBillService
    {
        private readonly AppDbContext _context;
        private readonly IAuditLogService _auditLogService;

        public BillService(AppDbContext context, IAuditLogService auditLogService)
        {
            _context = context;
            _auditLogService = auditLogService;
        }

        public async Task<IEnumerable<PendingBillDto>> GetPendingBillsAsync()
        {
            var now = DateTime.UtcNow;

            // Get all readings that are ready for billing
            var readyReadings = await _context.MeterReadings
                .Where(mr => mr.Status == "ReadyForBilling")
                .Include(mr => mr.Connection)
                    .ThenInclude(c => c.User)
                .Include(mr => mr.Connection)
                    .ThenInclude(c => c.UtilityType)
                .Include(mr => mr.Tariff) // Use stored tariff from reading time
                .Include(mr => mr.BillingCycle)
                .ToListAsync();

            var pendingBills = new List<PendingBillDto>();

            foreach (var reading in readyReadings)
            {
                if (reading.Connection == null)
                    continue;

                if (reading.Connection.User == null)
                    continue;

                if (reading.Connection.User.Status != "Active")
                    continue;

                if (reading.Connection.UtilityType == null)
                    continue;

                if (reading.Connection.UtilityType.Status != "Enabled")
                    continue;

                if (reading.BillingCycle == null)
                    continue;

                // Skip if billing cycle is not active
                if (!reading.BillingCycle.IsActive)
                    continue;

                // Filter to current billing period only (allow current period + 1 previous period for late entries)
                var (currentPeriodStart, currentPeriodEnd) = BillingCycleHelper.GetCurrentBillingPeriod(reading.BillingCycle, now);
                var (previousPeriodStart, previousPeriodEnd) = BillingCycleHelper.GetCurrentBillingPeriod(reading.BillingCycle, now.AddMonths(-1));

                // Only include readings from current period or previous period (for late entries/corrections)
                if (reading.ReadingDate < previousPeriodStart || reading.ReadingDate > currentPeriodEnd)
                    continue; 

                // Check if bill already exists for this connection and billing period
                var billingPeriod = BillingCycleHelper.GetBillingPeriodLabel(reading.ReadingDate);
                var existingBill = await _context.Bills
                    .Where(b => b.ConnectionId == reading.ConnectionId &&
                                b.BillingPeriod == billingPeriod)
                    .FirstOrDefaultAsync();

                if (existingBill != null)
                    continue;

                var tariff = reading.Tariff;
                if (tariff == null)
                    continue; 

                // Calculate expected amount
                var baseAmount = (reading.Consumption * tariff.BaseRate) + tariff.FixedCharge;
                var taxAmount = baseAmount * (tariff.TaxPercentage / 100);
                var expectedAmount = baseAmount + taxAmount;

                pendingBills.Add(new PendingBillDto
                {
                    ReadingId = reading.Id,
                    ConnectionId = reading.ConnectionId,
                    ConsumerName = reading.Connection.User.FullName,
                    UtilityName = reading.Connection.UtilityType.Name,
                    MeterNumber = reading.Connection.MeterNumber,
                    Units = reading.Consumption,
                    ExpectedAmount = expectedAmount,
                    Status = reading.Status,
                    ReadingDate = reading.ReadingDate,
                    BillingPeriod = billingPeriod
                });
            }

            return pendingBills;
        }

        public async Task<BillDto> GenerateBillAsync(string readingId, string userEmail)
        {
            var reading = await _context.MeterReadings
                .Include(mr => mr.Connection)
                    .ThenInclude(c => c.User)
                .Include(mr => mr.Connection)
                    .ThenInclude(c => c.UtilityType)
                .Include(mr => mr.Tariff)
                .Include(mr => mr.BillingCycle)
                .FirstOrDefaultAsync(mr => mr.Id == readingId);

            if (reading == null)
                throw new KeyNotFoundException("Meter reading not found");

            if (reading.Status != "ReadyForBilling")
                throw new InvalidOperationException("Reading is not ready for billing");

            // Check if user is active and not deleted
            if (reading.Connection.User == null)
                throw new InvalidOperationException("User not found");
            
            if (reading.Connection.User.Status != "Active")
                throw new InvalidOperationException("Cannot generate bill for an inactive or deleted consumer");

            // Check if utility type is enabled
            if (reading.Connection.UtilityType == null)
                throw new InvalidOperationException("Utility type not found");
            
            if (reading.Connection.UtilityType.Status != "Enabled")
                throw new InvalidOperationException("Cannot generate bill for a disabled utility type. Please enable the utility type first.");

            // Check if billing cycle is active
            if (reading.BillingCycle == null)
                throw new InvalidOperationException("Billing cycle not found");
            
            if (!reading.BillingCycle.IsActive)
                throw new InvalidOperationException("Cannot generate bill using an inactive billing cycle. Please activate the billing cycle first.");

            // Check if bill already exists
            var billingPeriod = BillingCycleHelper.GetBillingPeriodLabel(reading.ReadingDate);
            var existingBill = await _context.Bills
                .Where(b => b.ConnectionId == reading.ConnectionId &&
                            b.BillingPeriod == billingPeriod)
                .FirstOrDefaultAsync();

            if (existingBill != null)
                throw new InvalidOperationException("Bill already exists for this billing period");

            var tariff = reading.Tariff;
            if (tariff == null)
                throw new InvalidOperationException("Meter reading does not have a tariff assigned");
            
            // Calculate amounts
            var baseAmount = (reading.Consumption * tariff.BaseRate) + tariff.FixedCharge;
            var taxAmount = baseAmount * (tariff.TaxPercentage / 100);
            var totalAmount = baseAmount + taxAmount;

            // Calculate due date based on billing cycle
            var dueDate = reading.ReadingDate.AddDays(reading.BillingCycle.DueDateOffset);

            // Set GenerationDate to the generation day for consistent billing
            var generationDate = reading.ReadingDate;

            var bill = new Bill
            {
                ConnectionId = reading.ConnectionId,
                BillingPeriod = billingPeriod,
                GenerationDate = generationDate,
                DueDate = dueDate,
                PreviousReading = reading.PreviousReading,
                CurrentReading = reading.CurrentReading,
                Consumption = reading.Consumption,
                BaseAmount = baseAmount,
                TaxAmount = taxAmount,
                PenaltyAmount = 0,
                TotalAmount = totalAmount,
                Status = "Generated"
            };

            _context.Bills.Add(bill);

            // Update reading status to Billed
            reading.Status = "Billed";

            await _context.SaveChangesAsync();

            await _auditLogService.LogActionAsync(
                "BILL_GENERATE",
                $"Generated bill for connection {reading.Connection.MeterNumber}. Amount: {totalAmount:C}.",
                userEmail);

            // Write notification event to queue
            if (reading.Connection.User != null && reading.Connection.UtilityType != null)
            {
                var notificationEvent = new NotificationEvent
                {
                    UserId = reading.Connection.UserId,
                    BillId = bill.Id,
                    Type = "BillGenerated",
                    Title = "Bill Generated",
                    Message = $"Your {reading.Connection.UtilityType.Name} bill for {billingPeriod} has been generated. Amount: ₹{totalAmount:N2}. Due Date: {dueDate:dd MMM yyyy}",
                    Amount = totalAmount,
                    UtilityName = reading.Connection.UtilityType.Name,
                    BillingPeriod = billingPeriod,
                    DueDate = dueDate
                };

                await NotificationQueue.Channel.Writer.WriteAsync(notificationEvent);
            }

            return new BillDto
            {
                Id = bill.Id,
                ConnectionId = bill.ConnectionId,
                BillingPeriod = bill.BillingPeriod,
                GenerationDate = bill.GenerationDate,
                DueDate = bill.DueDate,
                PreviousReading = bill.PreviousReading,
                CurrentReading = bill.CurrentReading,
                Consumption = bill.Consumption,
                BaseAmount = bill.BaseAmount,
                TaxAmount = bill.TaxAmount,
                TotalAmount = bill.TotalAmount,
                Status = bill.Status
            };
        }

        public async Task<BillGenerationResponseDto> GenerateBillsBatchAsync(List<string> readingIds, string userEmail)
        {
            var response = new BillGenerationResponseDto
            {
                GeneratedCount = 0,
                FailedCount = 0,
                GeneratedBillIds = new List<string>(),
                Errors = new List<string>()
            };

            foreach (var readingId in readingIds)
            {
                try
                {
                    var bill = await GenerateBillAsync(readingId, userEmail);
                    response.GeneratedBillIds.Add(bill.Id);
                    response.GeneratedCount++;
                }
                catch (Exception ex)
                {
                    response.FailedCount++;
                    response.Errors.Add($"Reading {readingId}: {ex.Message}");
                }
            }

            return response;
        }

        public async Task<IEnumerable<BillDetailDto>> GetBillsByConnectionAsync(string connectionId)
        {
            var bills = await _context.Bills
                .Where(b => b.ConnectionId == connectionId)
                .Include(b => b.Connection)
                    .ThenInclude(c => c.User)
                .Include(b => b.Connection)
                    .ThenInclude(c => c.UtilityType)
                        .ThenInclude(u => u!.BillingCycle)
                .OrderByDescending(b => b.GenerationDate)
                .ToListAsync();

            var now = DateTime.UtcNow;
            bool hasChanges = false;

            foreach (var bill in bills)
            {
                var oldStatus = bill.Status;
                var oldTotalAmount = bill.TotalAmount;
                var oldPenaltyAmount = bill.PenaltyAmount;

                UpdateBillStatusInMemory(bill, now);

                // Check if values changed and need to be saved
                if (bill.Status != oldStatus || bill.TotalAmount != oldTotalAmount || bill.PenaltyAmount != oldPenaltyAmount)
                {
                    hasChanges = true;
                }
            }

            if (hasChanges)
            {
                await _context.SaveChangesAsync();
            }

            return bills.Select(MapToBillDetailDto);
        }

        public async Task<BillDetailDto?> GetBillByIdAsync(string id)
        {
            var bill = await _context.Bills
                .Include(b => b.Connection)
                    .ThenInclude(c => c.User)
                .Include(b => b.Connection)
                    .ThenInclude(c => c.UtilityType)
                        .ThenInclude(u => u!.BillingCycle)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (bill == null)
                return null;

            var now = DateTime.UtcNow;
            var oldStatus = bill.Status;
            var oldTotalAmount = bill.TotalAmount;
            var oldPenaltyAmount = bill.PenaltyAmount;

            UpdateBillStatusInMemory(bill, now);

            // Check if values changed and need to be saved
            if (bill.Status != oldStatus || bill.TotalAmount != oldTotalAmount || bill.PenaltyAmount != oldPenaltyAmount)
            {
                await _context.SaveChangesAsync();
            }

            return MapToBillDetailDto(bill);
        }

        public async Task<IEnumerable<BillDetailDto>> GetBillsForUserAsync(string userId)
        {
            var bills = await _context.Bills
                .Include(b => b.Connection)
                    .ThenInclude(c => c.User)
                .Include(b => b.Connection)
                    .ThenInclude(c => c.UtilityType)
                        .ThenInclude(u => u!.BillingCycle)
                .Where(b => b.Connection.UserId == userId)
                .OrderByDescending(b => b.GenerationDate)
                .ToListAsync();

            var now = DateTime.UtcNow;
            bool hasChanges = false;

            foreach (var bill in bills)
            {
                var oldStatus = bill.Status;
                var oldTotalAmount = bill.TotalAmount;
                var oldPenaltyAmount = bill.PenaltyAmount;

                UpdateBillStatusInMemory(bill, now);

                // Check if values changed and need to be saved
                if (bill.Status != oldStatus || bill.TotalAmount != oldTotalAmount || bill.PenaltyAmount != oldPenaltyAmount)
                {
                    hasChanges = true;
                }
            }

            if (hasChanges)
            {
                await _context.SaveChangesAsync();
            }

            return bills.Select(MapToBillDetailDto);
        }

        public async Task<BillDetailDto?> GetBillForUserByIdAsync(string billId, string userId)
        {
            var bill = await _context.Bills
                .Include(b => b.Connection)
                    .ThenInclude(c => c.User)
                .Include(b => b.Connection)
                    .ThenInclude(c => c.UtilityType)
                        .ThenInclude(u => u!.BillingCycle)
                .FirstOrDefaultAsync(b => b.Id == billId);

            if (bill == null)
                return null;

            if (bill.Connection.UserId != userId)
                return null;

            var now = DateTime.UtcNow;
            var oldStatus = bill.Status;
            var oldTotalAmount = bill.TotalAmount;
            var oldPenaltyAmount = bill.PenaltyAmount;

            UpdateBillStatusInMemory(bill, now);

            // Check if values changed and need to be saved
            if (bill.Status != oldStatus || bill.TotalAmount != oldTotalAmount || bill.PenaltyAmount != oldPenaltyAmount)
            {
                await _context.SaveChangesAsync();
            }

            return MapToBillDetailDto(bill);
        }

        public async Task<int> GetDueBillsCountForUserAsync(string userId)
        {
            var bills = await _context.Bills
                .Include(b => b.Connection)
                    .ThenInclude(c => c.UtilityType)
                        .ThenInclude(u => u!.BillingCycle)
                .Where(b => b.Connection.UserId == userId && b.Status != "Paid")
                .ToListAsync();

            var now = DateTime.UtcNow;
            bool hasChanges = false;

            foreach (var bill in bills)
            {
                var oldStatus = bill.Status;
                var oldTotalAmount = bill.TotalAmount;
                var oldPenaltyAmount = bill.PenaltyAmount;

                UpdateBillStatusInMemory(bill, now);

                // Check if values changed and need to be saved
                if (bill.Status != oldStatus || bill.TotalAmount != oldTotalAmount || bill.PenaltyAmount != oldPenaltyAmount)
                {
                    hasChanges = true;
                }
            }

            if (hasChanges)
            {
                await _context.SaveChangesAsync();
            }

            return bills.Count(b => b.Status == "Due" || b.Status == "Overdue");
        }

        private static void UpdateBillStatusInMemory(Bill bill, DateTime now)
        {
            if (bill.Status == "Paid")
                return;

            var originalAmount = bill.BaseAmount + bill.TaxAmount;

            // Get grace period from billing cycle
            var graceDays = bill.Connection?.UtilityType?.BillingCycle?.GracePeriod ?? 0;
            var overdueDate = bill.DueDate.AddDays(graceDays);

            if (now > overdueDate)
            {
                bill.Status = "Overdue";
                // Calculate daily accumulating penalty
                var fixedCharge = bill.Connection?.Tariff?.FixedCharge ?? 0;
                var daysOverdue = (now.Date - overdueDate.Date).Days;
                
                // Daily penalty rate: 1% of fixed charge per day (or minimum ₹1 per day)
                // This means after 30 days, penalty = 30% of fixed charge
                var dailyPenaltyRate = Math.Max(fixedCharge * 0.01m, 1.0m);
                var penaltyAmount = daysOverdue * dailyPenaltyRate;
                
                bill.PenaltyAmount = penaltyAmount;
                bill.TotalAmount = originalAmount + bill.PenaltyAmount;
            }
            else if (now >= bill.DueDate)
            {
                bill.Status = "Due";
                bill.PenaltyAmount = 0;
                bill.TotalAmount = originalAmount;
            }
            else
            {
                bill.Status = "Generated";
                bill.PenaltyAmount = 0;
                bill.TotalAmount = originalAmount;
            }
        }

        private static BillDetailDto MapToBillDetailDto(Bill b)
        {
            return new BillDetailDto
            {
                Id = b.Id,
                ConnectionId = b.ConnectionId,
                ConsumerName = b.Connection.User.FullName,
                UtilityName = b.Connection.UtilityType.Name,
                MeterNumber = b.Connection.MeterNumber,
                BillingPeriod = b.BillingPeriod,
                GenerationDate = b.GenerationDate,
                DueDate = b.DueDate,
                PreviousReading = b.PreviousReading,
                CurrentReading = b.CurrentReading,
                Consumption = b.Consumption,
                BaseAmount = b.BaseAmount,
                TaxAmount = b.TaxAmount,
                PenaltyAmount = b.PenaltyAmount,
                TotalAmount = b.TotalAmount,
                Status = b.Status
            };
        }
    }
}