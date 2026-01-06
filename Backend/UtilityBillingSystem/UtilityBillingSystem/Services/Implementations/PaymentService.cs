using AutoMapper;
using Microsoft.EntityFrameworkCore;
using UtilityBillingSystem.Data;
using UtilityBillingSystem.Models.Core;
using UtilityBillingSystem.Models.Dto.Payment;
using UtilityBillingSystem.Services.Interfaces;

namespace UtilityBillingSystem.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly AppDbContext _context;
        private readonly IAuditLogService _auditLogService;
        private readonly IMapper _mapper;

        public PaymentService(AppDbContext context, IAuditLogService auditLogService, IMapper mapper)
        {
            _context = context;
            _auditLogService = auditLogService;
            _mapper = mapper;
        }

        public async Task<PaymentDto> RecordPaymentAsync(string billId, CreatePaymentDto dto, string userId, string userEmail)
        {
            var bill = await _context.Bills
                .Include(b => b.Connection)
                    .ThenInclude(c => c.UtilityType)
                        .ThenInclude(u => u!.BillingCycle)
                .Include(b => b.Connection)
                    .ThenInclude(c => c.Tariff)
                .FirstOrDefaultAsync(b => b.Id == billId);

            if (bill == null)
                throw new KeyNotFoundException("Bill not found");

            // Ownership check - ensure the bill belongs to the current user
            if (bill.Connection == null || bill.Connection.UserId != userId)
                throw new UnauthorizedAccessException("You are not allowed to pay this bill");

            // Prevent double payments
            if (bill.Status == "Paid")
                throw new InvalidOperationException("Bill is already paid");

            var now = DateTime.UtcNow;

            // Recalculate penalty if overdue (using grace period)
            UpdateBillStatusInMemory(bill, now);

            var payment = new Payment
            {
                BillId = bill.Id,
                PaymentDate = now,
                Amount = bill.TotalAmount,
                PaymentMethod = dto.PaymentMethod,
                ReceiptNumber = dto.ReceiptNumber,
                UpiId = dto.UpiId,
                Status = "Completed"
            };

            _context.Payments.Add(payment);

            // Update bill status to Paid immediately
            bill.Status = "Paid";

            await _context.SaveChangesAsync();

            await _auditLogService.LogActionAsync(
                "PAYMENT_CREATE",
                $"Payment recorded for bill {bill.Id} amount {bill.TotalAmount:C} using {dto.PaymentMethod}.",
                userEmail);

            return _mapper.Map<PaymentDto>(payment);
        }

        public async Task<IEnumerable<PaymentHistoryDto>> GetPaymentHistoryForUserAsync(
            string userId,
            DateTime? startDate = null,
            DateTime? endDate = null,
            string? utilityTypeId = null)
        {
            var query = _context.Payments
                .Include(p => p.Bill)
                    .ThenInclude(b => b.Connection)
                        .ThenInclude(c => c.UtilityType)
                .Include(p => p.Bill)
                    .ThenInclude(b => b.Connection)
                        .ThenInclude(c => c.User)
                .Where(p => p.Bill.Connection.UserId == userId)
                .AsQueryable();

            if (startDate.HasValue)
            {
                query = query.Where(p => p.PaymentDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(p => p.PaymentDate <= endDate.Value);
            }

            if (!string.IsNullOrEmpty(utilityTypeId))
            {
                query = query.Where(p => p.Bill.Connection.UtilityTypeId == utilityTypeId);
            }

            var payments = await query
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            return _mapper.Map<IEnumerable<PaymentHistoryDto>>(payments);
        }

        public async Task<decimal> GetOutstandingBalanceForUserAsync(string userId)
        {
            var now = DateTime.UtcNow;

            // Get all unpaid bills (Generated, Due, Overdue) for the user
            var outstanding = await _context.Bills
                .Include(b => b.Connection)
                    .ThenInclude(c => c.UtilityType)
                        .ThenInclude(u => u!.BillingCycle)
                .Include(b => b.Connection)
                    .ThenInclude(c => c.Tariff)
                .Where(b => b.Connection.UserId == userId && b.Status != "Paid")
                .ToListAsync();

            bool hasChanges = false;
            foreach (var bill in outstanding)
            {
                var oldStatus = bill.Status;
                var oldTotalAmount = bill.TotalAmount;
                var oldPenaltyAmount = bill.PenaltyAmount;
                
                UpdateBillStatusInMemory(bill, now);
                
                if (bill.Status != oldStatus || bill.TotalAmount != oldTotalAmount || bill.PenaltyAmount != oldPenaltyAmount)
                {
                    hasChanges = true;
                }
            }

            if (hasChanges)
            {
                await _context.SaveChangesAsync();
            }

            // Return sum of all unpaid bills (includes Generated, Due, and Overdue)
            return outstanding
                .Sum(b => b.TotalAmount);
        }

        public async Task<decimal> GetMonthlySpendingForUserAsync(string userId, DateTime monthDateUtc)
        {
            var now = DateTime.UtcNow;
            var monthStart = new DateTime(monthDateUtc.Year, monthDateUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var monthEnd = monthStart.AddMonths(1);

            var bills = await _context.Bills
                .Include(b => b.Connection)
                    .ThenInclude(c => c.UtilityType)
                        .ThenInclude(u => u!.BillingCycle)
                .Include(b => b.Connection)
                    .ThenInclude(c => c.Tariff)
                .Where(b => b.Connection.UserId == userId &&
                            b.GenerationDate >= monthStart &&
                            b.GenerationDate < monthEnd)
                .ToListAsync();

            bool hasChanges = false;
            foreach (var bill in bills)
            {
                var oldStatus = bill.Status;
                var oldTotalAmount = bill.TotalAmount;
                var oldPenaltyAmount = bill.PenaltyAmount;
                
                UpdateBillStatusInMemory(bill, now);
                
                if (bill.Status != oldStatus || bill.TotalAmount != oldTotalAmount || bill.PenaltyAmount != oldPenaltyAmount)
                {
                    hasChanges = true;
                }
            }

            if (hasChanges)
            {
                await _context.SaveChangesAsync();
            }

            // Return sum of ALL bills generated in this month (paid and unpaid) to show total monthly spending
            return bills.Sum(b => b.TotalAmount);
        }

        public async Task<IEnumerable<(string MonthLabel, decimal TotalConsumption)>> GetMonthlyConsumptionForUserAsync(string userId)
        {
            // Use MeterReadings to compute consumption per month across all utilities
            var readings = await _context.MeterReadings
                .Include(mr => mr.Connection)
                .Where(mr => mr.Connection.UserId == userId)
                .ToListAsync();

            var grouped = readings
                .GroupBy(mr => new { mr.ReadingDate.Year, mr.ReadingDate.Month })
                .OrderBy(g => g.Key.Year)
                .ThenBy(g => g.Key.Month)
                .Select(g => (
                    MonthLabel: $"{g.Key.Year}-{g.Key.Month:00}",
                    TotalConsumption: g.Sum(x => x.Consumption)
                ));

            return grouped;
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
    }
}


