namespace UtilityBillingSystem.Models.Dto.Bill
{
    public class PendingBillDto
    {
        public string ReadingId { get; set; } = string.Empty;
        public string ConnectionId { get; set; } = string.Empty;
        public string ConsumerName { get; set; } = string.Empty;
        public string UtilityName { get; set; } = string.Empty;
        public string MeterNumber { get; set; } = string.Empty;
        public decimal Units { get; set; } // Consumption
        public decimal ExpectedAmount { get; set; } // Calculated based on tariff
        public string Status { get; set; } = string.Empty;
        public DateTime ReadingDate { get; set; }
        public string BillingPeriod { get; set; } = string.Empty;
    }
}

