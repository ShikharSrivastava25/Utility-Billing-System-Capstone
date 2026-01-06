namespace UtilityBillingSystem.Models.Dto.MeterReading
{
    public class ConnectionForReadingDto
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string ConsumerName { get; set; } = string.Empty;
        public string UtilityTypeId { get; set; } = string.Empty;
        public string UtilityName { get; set; } = string.Empty;
        public string TariffId { get; set; } = string.Empty;
        public string MeterNumber { get; set; } = string.Empty;
        public decimal? PreviousReading { get; set; } // Last reading's current reading value
        public string BillingCycleId { get; set; } = string.Empty;
        public string BillingCycleName { get; set; } = string.Empty;
    }
}

