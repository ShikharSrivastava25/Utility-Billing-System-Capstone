namespace UtilityBillingSystem.Models.Dto.MeterReading
{
    public class MeterReadingDto
    {
        public string Id { get; set; } = string.Empty;
        public string ConnectionId { get; set; } = string.Empty;
        public decimal PreviousReading { get; set; }
        public decimal CurrentReading { get; set; }
        public decimal Consumption { get; set; }
        public DateTime ReadingDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string RecordedBy { get; set; } = string.Empty;
        public string BillingCycleId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
