using UtilityBillingSystem.Models.Dto.Bill;

namespace UtilityBillingSystem.Services
{
    public class BillCalculationService
    {
        public BillCalculationResultDto CalculateBillAmount(decimal consumption, decimal baseRate, decimal fixedCharge, decimal taxPercentage)
        {
            if (consumption < 0)
                throw new ArgumentException("Consumption cannot be negative", nameof(consumption));

            if (baseRate < 0)
                throw new ArgumentException("Base rate cannot be negative", nameof(baseRate));

            if (fixedCharge < 0)
                throw new ArgumentException("Fixed charge cannot be negative", nameof(fixedCharge));

            if (taxPercentage < 0 || taxPercentage > 100)
                throw new ArgumentException("Tax percentage must be between 0 and 100", nameof(taxPercentage));

            var baseAmount = (consumption * baseRate) + fixedCharge;
            var taxAmount = baseAmount * (taxPercentage / 100);
            var totalAmount = baseAmount + taxAmount;

            return new BillCalculationResultDto
            {
                Consumption = consumption,
                BaseRate = baseRate,
                FixedCharge = fixedCharge,
                BaseAmount = baseAmount,
                TaxPercentage = taxPercentage,
                TaxAmount = taxAmount,
                TotalAmount = totalAmount
            };
        }

        public decimal CalculatePenalty(decimal fixedCharge, int daysOverdue)
        {
            if (fixedCharge < 0)
                throw new ArgumentException("Fixed charge cannot be negative", nameof(fixedCharge));

            if (daysOverdue < 0)
                throw new ArgumentException("Days overdue cannot be negative", nameof(daysOverdue));

            // Daily penalty rate: 1% of fixed charge per day (or minimum ₹1 per day)
            var dailyPenaltyRate = Math.Max(fixedCharge * 0.01m, 1.0m);
            var penaltyAmount = daysOverdue * dailyPenaltyRate;

            return penaltyAmount;
        }

        public string DetermineBillStatus(DateTime dueDate, DateTime now, int gracePeriod)
        {
            if (gracePeriod < 0)
                throw new ArgumentException("Grace period cannot be negative", nameof(gracePeriod));

            var overdueDate = dueDate.AddDays(gracePeriod);

            if (now < dueDate)
            {
                return "Generated";
            }
            else if (now >= dueDate && now <= overdueDate)
            {
                return "Due";
            }
            else
            {
                return "Overdue";
            }
        }

        public bool CanGenerateBill(string readingStatus)
        {
            if (string.IsNullOrWhiteSpace(readingStatus))
                throw new ArgumentException("Reading status cannot be null or empty", nameof(readingStatus));

            return readingStatus == "ReadyForBilling";
        }

        public decimal ValidateAndCalculateConsumption(decimal currentReading, decimal previousReading)
        {
            if (currentReading < 0)
                throw new ArgumentException("Current reading cannot be negative", nameof(currentReading));

            if (previousReading < 0)
                throw new ArgumentException("Previous reading cannot be negative", nameof(previousReading));

            if (currentReading < previousReading)
                throw new ArgumentException("Current reading cannot be less than previous reading", nameof(currentReading));

            return currentReading - previousReading;
        }

        public DateTime CalculateDueDate(DateTime readingDate, int dueDateOffset)
        {
            if (dueDateOffset < 0)
                throw new ArgumentException("Due date offset cannot be negative", nameof(dueDateOffset));

            return readingDate.AddDays(dueDateOffset);
        }

        public int CalculateDaysOverdue(DateTime dueDate, DateTime now, int gracePeriod)
        {
            if (gracePeriod < 0)
                throw new ArgumentException("Grace period cannot be negative", nameof(gracePeriod));

            var overdueDate = dueDate.AddDays(gracePeriod);

            if (now <= overdueDate)
                return 0;

            return (now.Date - overdueDate.Date).Days;
        }
    }
}

