namespace UtilityBillingSystem.Models.Dto.MeterReading
{
    public class MeterReadingResponseDto
    {
        public string Id { get; set; } = string.Empty;
        public string ConnectionId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string ConsumerName { get; set; } = string.Empty;
        public string UtilityName { get; set; } = string.Empty;
        public string MeterNumber { get; set; } = string.Empty;
        public decimal PreviousReading { get; set; }
        public decimal CurrentReading { get; set; }
        public decimal Consumption { get; set; }
        public DateTime ReadingDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string RecordedBy { get; set; } = string.Empty;
        public string BillingCycleId { get; set; } = string.Empty;
        public string Month { get; set; } = string.Empty; // e.g., "March 2025"
        public DateTime CreatedAt { get; set; }
    }
}

