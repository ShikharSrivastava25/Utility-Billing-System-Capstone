using Microsoft.EntityFrameworkCore;
using UtilityBillingSystem.Data;
using UtilityBillingSystem.Models.Core;
using UtilityBillingSystem.Models.Dto.Report;
using UtilityBillingSystem.Models.Dto.Consumer;
using UtilityBillingSystem.Services.Interfaces;

namespace UtilityBillingSystem.Services
{
    public class ReportService : IReportService
    {
        private readonly AppDbContext _context;
        private readonly IPaymentService _paymentService;
        private readonly IConnectionService _connectionService;
        private readonly IBillService _billService;

        public ReportService(AppDbContext context, IPaymentService paymentService, IConnectionService connectionService, IBillService billService)
        {
            _context = context;
            _paymentService = paymentService;
            _connectionService = connectionService;
            _billService = billService;
        }

        public async Task<ReportSummaryDto> GetReportSummaryAsync()
        {
            var activeBillingOfficers = await _context.Users
                .Join(
                    _context.UserRoles,
                    u => u.Id,
                    ur => ur.UserId,
                    (u, ur) => new { u, ur.RoleId }
                )
                .Join(
                    _context.Roles,
                    u => u.RoleId,
                    r => r.Id,
                    (u, r) => new { u.u, r.Name }
                )
                .Where(x => x.Name == "Billing Officer" && x.u.Status == "Active")
                .CountAsync();

            var activeAccountOfficers = await _context.Users
                .Join(
                    _context.UserRoles,
                    u => u.Id,
                    ur => ur.UserId,
                    (u, ur) => new { u, ur.RoleId }
                )
                .Join(
                    _context.Roles,
                    u => u.RoleId,
                    r => r.Id,
                    (u, r) => new { u.u, r.Name }
                )
                .Where(x => x.Name == "Account Officer" && x.u.Status == "Active")
                .CountAsync();

            var activeConsumers = await _context.Users
                .Join(
                    _context.UserRoles,
                    u => u.Id,
                    ur => ur.UserId,
                    (u, ur) => new { u, ur.RoleId }
                )
                .Join(
                    _context.Roles,
                    u => u.RoleId,
                    r => r.Id,
                    (u, r) => new { u.u, r.Name }
                )
                .Where(x => x.Name == "Consumer" && x.u.Status == "Active")
                .CountAsync();

            var pendingUtilityRequests = await _context.UtilityRequests
                .Where(ur => ur.Status == "Pending")
                .CountAsync();

            return new ReportSummaryDto
            {
                ActiveBillingOfficers = activeBillingOfficers,
                ActiveAccountOfficers = activeAccountOfficers,
                TotalConsumers = activeConsumers,
                PendingUtilityRequests = pendingUtilityRequests
            };
        }

        public async Task<IEnumerable<OverdueBillDto>> GetOverdueBillsAsync()
        {
            var now = DateTime.UtcNow;

            var unpaidBills = await _context.Bills
                .Where(b => b.Status != "Paid")
                .Include(b => b.Connection)
                    .ThenInclude(c => c.User)
                .Include(b => b.Connection)
                    .ThenInclude(c => c.UtilityType)
                        .ThenInclude(u => u!.BillingCycle)
                .Include(b => b.Connection)
                    .ThenInclude(c => c.Tariff)
                .ToListAsync();

            bool hasChanges = false;
            foreach (var bill in unpaidBills)
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

            return unpaidBills
                .Where(b => b.Status == "Overdue")
                .Select(b => new OverdueBillDto
                {
                    BillId = b.Id,
                    ConsumerName = b.Connection.User.FullName,
                    UtilityName = b.Connection.UtilityType.Name,
                    Amount = b.TotalAmount,
                    DueDate = b.DueDate
                })
                .OrderBy(o => o.DueDate)
                .ToList();
        }

        public async Task<IEnumerable<ConsumptionDataDto>> GetConsumptionByUtilityAsync()
        {
            return await _context.Bills
                .Join(
                    _context.Connections,
                    b => b.ConnectionId,
                    c => c.Id,
                    (b, c) => new { b.Consumption, c.UtilityTypeId }
                )
                .Join(
                    _context.UtilityTypes,
                    x => x.UtilityTypeId,
                    ut => ut.Id,
                    (x, ut) => new { x.Consumption, ut.Name }
                )
                .GroupBy(x => x.Name)
                .Select(g => new ConsumptionDataDto
                {
                    UtilityName = g.Key,
                    Consumption = g.Sum(x => x.Consumption)
                })
                .OrderByDescending(c => c.Consumption)
                .ToListAsync();
        }

        public async Task<IEnumerable<MonthlyRevenueDto>> GetMonthlyRevenueAsync()
        {
            var monthlyRevenueData = await _context.Payments
                .Where(p => p.Status == "Completed")
                .GroupBy(p => new { p.PaymentDate.Year, p.PaymentDate.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Revenue = g.Sum(p => p.Amount)
                })
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ToListAsync();

            var monthlyRevenue = monthlyRevenueData.Select(x => new MonthlyRevenueDto
            {
                Month = new DateTime(x.Year, x.Month, 1).ToString("MMMM yyyy"),
                Revenue = x.Revenue
            }).ToList();

            return monthlyRevenue;
        }

        public async Task<IEnumerable<AverageConsumptionDto>> GetAverageConsumptionAsync()
        {
            var results = await _context.Bills
                .Where(b => b.Consumption > 0)
                .Join(
                    _context.Connections,
                    b => b.ConnectionId,
                    c => c.Id,
                    (b, c) => new { b.Consumption, c.UtilityTypeId }
                )
                .Join(
                    _context.UtilityTypes,
                    x => x.UtilityTypeId,
                    ut => ut.Id,
                    (x, ut) => new { x.Consumption, ut.Name }
                )
                .GroupBy(x => x.Name)
                .Select(g => new
                {
                    UtilityName = g.Key,
                    AverageConsumption = g.Average(x => x.Consumption)
                })
                .OrderByDescending(c => c.AverageConsumption)
                .ToListAsync();

            // Round to 2 decimal places
            return results.Select(r => new AverageConsumptionDto
            {
                UtilityName = r.UtilityName,
                AverageConsumption = Math.Round(r.AverageConsumption, 2)
            }).ToList();
        }

        public async Task<IEnumerable<ConnectionsByUtilityDto>> GetConnectionsByUtilityAsync()
        {
            return await _context.Connections
                .Join(
                    _context.UtilityTypes,
                    c => c.UtilityTypeId,
                    ut => ut.Id,
                    (c, ut) => new { ut.Name }
                )
                .GroupBy(x => x.Name)
                .Select(g => new ConnectionsByUtilityDto
                {
                    UtilityName = g.Key,
                    ConnectionCount = g.Count()
                })
                .OrderByDescending(c => c.ConnectionCount)
                .ToListAsync();
        }

        public async Task<IEnumerable<object>> GetMyConsumptionAsync(string userId, string? utilityTypeId = null)
        {
            var query = _context.MeterReadings
                .Include(mr => mr.Connection)
                    .ThenInclude(c => c.UtilityType)
                .Include(mr => mr.Connection)
                    .ThenInclude(c => c.Tariff)
                .Where(mr => mr.Connection.UserId == userId)
                .AsQueryable();

            if (!string.IsNullOrEmpty(utilityTypeId))
            {
                query = query.Where(mr => mr.Connection.UtilityTypeId == utilityTypeId);
            }

            var readings = await query.ToListAsync();

            // If no specific utility selected, return combined consumption trend
            if (string.IsNullOrEmpty(utilityTypeId))
            {
                var grouped = readings
                    .GroupBy(mr => new { mr.ReadingDate.Year, mr.ReadingDate.Month })
                    .OrderBy(g => g.Key.Year)
                    .ThenBy(g => g.Key.Month)
                    .Select(g => new
                    {
                        Month = $"{g.Key.Year}-{g.Key.Month:00}",
                        TotalConsumption = g.Sum(x => x.Consumption)
                    })
                    .ToList();

                return grouped;
            }

            var table = readings
                .GroupBy(mr => new { mr.ReadingDate.Year, mr.ReadingDate.Month })
                .OrderBy(g => g.Key.Year)
                .ThenBy(g => g.Key.Month)
                .Select(g =>
                {
                    var monthLabel = $"{g.Key.Year}-{g.Key.Month:00}";
                    var units = g.Sum(x => x.Consumption);

                    var firstTariff = g.FirstOrDefault()?.Connection.Tariff;
                    decimal estimatedCost = 0;

                    if (firstTariff != null)
                    {
                        var baseAmount = (units * firstTariff.BaseRate) + firstTariff.FixedCharge;
                        var taxAmount = baseAmount * (firstTariff.TaxPercentage / 100);
                        estimatedCost = baseAmount + taxAmount;
                    }

                    return new ConsumptionTableRow
                    {
                        Month = monthLabel,
                        Units = units,
                        EstimatedCost = estimatedCost
                    };
                })
                .ToList();

            return table;
        }

        public async Task<ConsumerDashboardResponse> GetConsumerDashboardAsync(string userId)
        {
            var now = DateTime.UtcNow;
            var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var monthEnd = monthStart.AddMonths(1);
            
            // Load all unpaid bills for outstanding balance calculation
            var allUnpaidBills = await _context.Bills
                .Include(b => b.Connection)
                    .ThenInclude(c => c.UtilityType)
                        .ThenInclude(u => u!.BillingCycle)
                .Include(b => b.Connection)
                    .ThenInclude(c => c.Tariff)
                .Where(b => b.Connection.UserId == userId && b.Status != "Paid")
                .ToListAsync();

            // Recalculate penalties for unpaid bills
            bool hasChanges = false;
            foreach (var bill in allUnpaidBills)
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

            // Calculate outstanding balance: sum of all unpaid bills (Generated, Due, Overdue)
            var outstanding = allUnpaidBills
                .Sum(b => b.TotalAmount);

            // Calculate monthly spending: sum of ALL bills (paid and unpaid) generated this month
            var monthlySpending = await _context.Bills
                .Where(b => b.Connection.UserId == userId &&
                           b.GenerationDate >= monthStart &&
                           b.GenerationDate < monthEnd)
                .SumAsync(b => b.TotalAmount);

            var consumption = await _paymentService.GetMonthlyConsumptionForUserAsync(userId);
            var connections = await _connectionService.GetConnectionsForUserAsync(userId);
            var activeConnectionsCount = connections.Count(c => c.Status == "Active");
            var dueBillsCount = await _billService.GetDueBillsCountForUserAsync(userId);

            return new ConsumerDashboardResponse
            {
                OutstandingBalance = outstanding,
                MonthlySpending = monthlySpending,
                ActiveConnections = activeConnectionsCount,
                DueBillsCount = dueBillsCount,
                ConsumptionTrend = consumption.Select(c => new ConsumptionPoint
                {
                    Month = c.MonthLabel,
                    TotalConsumption = c.TotalConsumption
                })
            };
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

