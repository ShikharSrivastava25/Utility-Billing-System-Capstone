using Microsoft.EntityFrameworkCore;
using UtilityBillingSystem.Data;
using UtilityBillingSystem.Models.Core;

namespace UtilityBillingSystem.Services.Helpers
{
    public static class DashboardMetricsHelper
    {
       
        public static async Task<decimal> CalculateTotalRevenueAsync(AppDbContext context)
        {
            return await context.Bills
                .Where(b => b.Status == "Paid")
                .SumAsync(b => b.TotalAmount);
        }

        
        public static async Task<decimal> CalculateOutstandingDuesAsync(AppDbContext context)
        {
            var now = DateTime.UtcNow;

            // Get all unpaid bills
            var unpaidBills = await context.Bills
                .Include(b => b.Connection)
                    .ThenInclude(c => c.UtilityType)
                        .ThenInclude(u => u!.BillingCycle)
                .Include(b => b.Connection)
                    .ThenInclude(c => c.Tariff)
                .Where(b => b.Status != "Paid")
                .ToListAsync();

            foreach (var bill in unpaidBills)
            {
                UpdateBillStatusInMemory(bill, now);
            }

            // Calculate outstanding balance: sum of all unpaid bills (Generated, Due, Overdue)
            // This represents the total amount owed by all consumers
            return unpaidBills
                .Sum(b => b.TotalAmount);
        }

        public static async Task<decimal> CalculateTotalConsumptionAsync(AppDbContext context)
        {
            return await context.Bills
                .SumAsync(b => b.Consumption);
        }

        public static void UpdateBillStatusInMemory(Bill bill, DateTime now)
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

