using UtilityBillingSystem.Models.Core;

namespace UtilityBillingSystem.Services.Helpers
{
    public static class BillingCycleHelper
    {
        public static (DateTime periodStart, DateTime periodEnd) GetCurrentBillingPeriod(BillingCycle cycle, DateTime? referenceDate = null)
        {
            var now = referenceDate ?? DateTime.UtcNow;
            var year = now.Year;
            var month = now.Month;
            var day = now.Day;
            var generationDay = Math.Min(cycle.GenerationDay, DateTime.DaysInMonth(year, month));

            // If we haven't reached the generation day yet, we're still in the previous month's billing period
            if (day < generationDay)
            {
                var previousMonth = now.AddMonths(-1);
                year = previousMonth.Year;
                month = previousMonth.Month;
            }

            // Period starts on the generation day of the determined month
            var periodStart = new DateTime(year, month, Math.Min(cycle.GenerationDay, DateTime.DaysInMonth(year, month)));

            // Period ends on the day before generation day of next month
            var nextMonth = periodStart.AddMonths(1);
            var periodEnd = new DateTime(nextMonth.Year, nextMonth.Month, Math.Min(cycle.GenerationDay, DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month))).AddDays(-1);

            return (periodStart, periodEnd);
        }

        public static bool IsDateInBillingPeriod(DateTime date, BillingCycle cycle)
        {
            var (periodStart, periodEnd) = GetCurrentBillingPeriod(cycle, date);
            return date >= periodStart && date <= periodEnd;
        }

        public static string GetBillingPeriodLabel(DateTime periodStart)
        {
            var consumptionMonth = periodStart.AddMonths(-1);
            return consumptionMonth.ToString("MMMM yyyy");
        }

        public static string GetBillingPeriodLabelForDate(DateTime date, BillingCycle cycle)
        {
            var (periodStart, _) = GetCurrentBillingPeriod(cycle, date);
            return GetBillingPeriodLabel(periodStart);
        }
    }
}


