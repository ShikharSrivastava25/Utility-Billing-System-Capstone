namespace UtilityBillingSystem.Models.Dto.MeterReading
{
    public class MeterReadingRequestDto
    {
        public string ConnectionId { get; set; } = string.Empty;
        public decimal CurrentReading { get; set; }
        public DateTime ReadingDate { get; set; }
    }
}

