using AutoMapper;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using UtilityBillingSystem.Data;
using UtilityBillingSystem.Models.Core;
using UtilityBillingSystem.Models.Dto.AccountOfficer;
using UtilityBillingSystem.Models.Dto.Report;
using UtilityBillingSystem.Services.Interfaces;
using UtilityBillingSystem.Services.Helpers;

namespace UtilityBillingSystem.Services
{
    public class AccountOfficerService : IAccountOfficerService
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;

        public AccountOfficerService(AppDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<AccountOfficerDashboardDto> GetDashboardSummaryAsync()
        {
            var now = DateTime.UtcNow;

            // Use helper methods for shared calculations
            var totalRevenue = await DashboardMetricsHelper.CalculateTotalRevenueAsync(_context);
            var outstandingDues = await DashboardMetricsHelper.CalculateOutstandingDuesAsync(_context);
            var totalConsumption = await DashboardMetricsHelper.CalculateTotalConsumptionAsync(_context);

            var unpaidBills = await _context.Bills
                .Include(b => b.Connection)
                    .ThenInclude(c => c.UtilityType)
                        .ThenInclude(u => u!.BillingCycle)
                .Include(b => b.Connection)
                    .ThenInclude(c => c.Tariff)
                .Where(b => b.Status != "Paid")
                .ToListAsync();

            foreach (var bill in unpaidBills)
            {
                DashboardMetricsHelper.UpdateBillStatusInMemory(bill, now);
            }

            var unpaidCount = unpaidBills
                .Count(b => b.Status == "Due" || b.Status == "Overdue" || b.Status == "Generated");

            return new AccountOfficerDashboardDto
            {
                TotalRevenue = totalRevenue,
                UnpaidBillsCount = unpaidCount,
                OutstandingDues = outstandingDues,
                TotalConsumption = totalConsumption
            };
        }

        public async Task<IEnumerable<MonthlyRevenueDto>> GetMonthlyRevenueByBillingDateAsync(
            DateTime? startDate = null, 
            DateTime? endDate = null, 
            int? month = null, 
            int? year = null)
        {
            var query = _context.Payments.Where(p => p.Status == "Completed");

            // Apply date range filter if provided
            if (startDate.HasValue)
            {
                query = query.Where(p => p.PaymentDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                // Add one day to include the entire end date
                var endDateInclusive = endDate.Value.AddDays(1);
                query = query.Where(p => p.PaymentDate < endDateInclusive);
            }
            // Apply month/year filter if provided
            else if (month.HasValue && year.HasValue)
            {
                query = query.Where(p => p.PaymentDate.Year == year.Value && p.PaymentDate.Month == month.Value);
            }

            // Group by payment month and sum amounts
            var monthlyRevenueData = await query
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

            return monthlyRevenueData.Select(x => new MonthlyRevenueDto
            {
                Month = new DateTime(x.Year, x.Month, 1).ToString("MMMM yyyy"),
                Revenue = x.Revenue
            });
        }

        private DateTime? ParseBillingPeriod(string billingPeriod)
        {
            if (string.IsNullOrWhiteSpace(billingPeriod))
                return null;

            try
            {
                if (DateTime.TryParseExact(billingPeriod, "MMMM yyyy", 
                    System.Globalization.CultureInfo.InvariantCulture, 
                    System.Globalization.DateTimeStyles.None, out DateTime result))
                {
                    return result;
                }
            }
            catch
            {
            }

            return null;
        }

        public async Task<IEnumerable<RecentPaymentDto>> GetRecentPaymentsAsync(int count = 5)
        {
            var payments = await _context.Payments
                .Include(p => p.Bill)
                    .ThenInclude(b => b.Connection)
                        .ThenInclude(c => c.User)
                .Include(p => p.Bill)
                    .ThenInclude(b => b.Connection)
                        .ThenInclude(c => c.UtilityType)
                .Where(p => p.Status == "Completed")
                .OrderByDescending(p => p.PaymentDate)
                .Take(count)
                .ToListAsync();

            return _mapper.Map<IEnumerable<RecentPaymentDto>>(payments);
        }

        public async Task<IEnumerable<OutstandingByUtilityDto>> GetOutstandingByUtilityAsync()
        {
            var now = DateTime.UtcNow;

            var unpaidBills = await _context.Bills
                .Include(b => b.Connection)
                    .ThenInclude(c => c.UtilityType)
                        .ThenInclude(u => u!.BillingCycle)
                .Include(b => b.Connection)
                    .ThenInclude(c => c.Tariff)
                .Where(b => b.Status != "Paid")
                .ToListAsync();

            // Update status in memory using helper method
            foreach (var bill in unpaidBills)
            {
                DashboardMetricsHelper.UpdateBillStatusInMemory(bill, now);
            }

            // Only include Due and Overdue bills (exclude Generated bills that haven't become due yet)
            var outstandingByUtility = unpaidBills
                .Where(b => b.Status == "Due" || b.Status == "Overdue")
                .GroupBy(b => b.Connection.UtilityType.Name)
                .Select(g => new OutstandingByUtilityDto
                {
                    UtilityName = g.Key,
                    OutstandingAmount = g.Sum(b => b.TotalAmount)
                })
                .OrderByDescending(x => x.OutstandingAmount)
                .ToList();

            return outstandingByUtility;
        }

        public async Task<PagedResult<PaymentAuditDto>> GetAllPaymentsAsync(int page, int pageSize)
        {
            var query = _context.Payments
                .Include(p => p.Bill)
                    .ThenInclude(b => b.Connection)
                        .ThenInclude(c => c.User)
                .Include(p => p.Bill)
                    .ThenInclude(b => b.Connection)
                        .ThenInclude(c => c.UtilityType)
                .Where(p => p.Status == "Completed")
                .AsQueryable();

            var totalCount = await query.CountAsync();

            var payments = await query
                .OrderByDescending(p => p.PaymentDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var data = _mapper.Map<IEnumerable<PaymentAuditDto>>(payments).ToList();

            return new PagedResult<PaymentAuditDto>
            {
                Data = data,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<PagedResult<OutstandingBillDto>> GetOutstandingBillsAsync(string? statusFilter, int page, int pageSize)
        {
            var now = DateTime.UtcNow;

            // Get all unpaid bills
            var unpaidBills = await _context.Bills
                .Include(b => b.Connection)
                    .ThenInclude(c => c.User)
                .Include(b => b.Connection)
                    .ThenInclude(c => c.UtilityType)
                        .ThenInclude(u => u!.BillingCycle)
                .Include(b => b.Connection)
                    .ThenInclude(c => c.Tariff)
                .Where(b => b.Status != "Paid")
                .ToListAsync();

            foreach (var bill in unpaidBills)
            {
                DashboardMetricsHelper.UpdateBillStatusInMemory(bill, now);
            }

            // Filter by status
            var filteredBills = unpaidBills.AsQueryable();
            
            if (!string.IsNullOrWhiteSpace(statusFilter) && statusFilter.ToLower() != "all")
            {
                filteredBills = filteredBills.Where(b => b.Status.ToLower() == statusFilter.ToLower());
            }
            else
            {
                // Show only Due and Overdue bills (exclude Generated)
                filteredBills = filteredBills.Where(b => b.Status == "Due" || b.Status == "Overdue");
            }

            var sortedBills = filteredBills
                .OrderByDescending(b => ParseBillingPeriod(b.BillingPeriod) ?? DateTime.MinValue)
                .ToList();

            // Get total count
            var totalCount = sortedBills.Count;

            var pagedBills = sortedBills
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var mappedBills = _mapper.Map<IEnumerable<OutstandingBillDto>>(pagedBills).ToList();

            return new PagedResult<OutstandingBillDto>
            {
                Data = mappedBills,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<PagedResult<ConsumerBillingSummaryDto>> GetConsumerBillingSummaryAsync(int page, int pageSize)
        {
            var now = DateTime.UtcNow;

            // Get all bills with connections and users
            var bills = await _context.Bills
                .Include(b => b.Connection)
                    .ThenInclude(c => c.User)
                .Include(b => b.Connection)
                    .ThenInclude(c => c.UtilityType)
                        .ThenInclude(u => u!.BillingCycle)
                .Include(b => b.Connection)
                    .ThenInclude(c => c.Tariff)
                .ToListAsync();

            foreach (var bill in bills)
            {
                DashboardMetricsHelper.UpdateBillStatusInMemory(bill, now);
            }

            // Group by consumer and calculate aggregates
            var summaries = bills
                .GroupBy(b => new { b.Connection.UserId, b.Connection.User.FullName })
                .Select(g => new ConsumerBillingSummaryDto
                {
                    ConsumerId = g.Key.UserId,
                    ConsumerName = g.Key.FullName,
                    TotalBilled = g.Sum(b => b.TotalAmount),
                    TotalPaid = g.Where(b => b.Status == "Paid").Sum(b => b.TotalAmount),
                    OutstandingBalance = g.Where(b => b.Status != "Paid").Sum(b => b.TotalAmount),
                    OverdueCount = g.Count(b => b.Status == "Overdue")
                })
                .AsQueryable();

            // Convert to list and sort by consumer name (case-insensitive)
            var sortedSummaries = summaries
                .OrderBy(s => s.ConsumerName, StringComparer.OrdinalIgnoreCase)
                .ToList();

            // Get total count
            var totalCount = sortedSummaries.Count;

            var pagedSummaries = sortedSummaries
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new PagedResult<ConsumerBillingSummaryDto>
            {
                Data = pagedSummaries,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

    }
}

