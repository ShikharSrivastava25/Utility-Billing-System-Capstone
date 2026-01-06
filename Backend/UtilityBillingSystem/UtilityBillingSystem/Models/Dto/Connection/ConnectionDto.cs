namespace UtilityBillingSystem.Models.Dto.Connection
{
    public class ConnectionDto
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string? UserName { get; set; }
        public string UtilityTypeId { get; set; } = string.Empty;
        public string? UtilityTypeName { get; set; }
        public string TariffId { get; set; } = string.Empty;
        public string? TariffName { get; set; }
        public string MeterNumber { get; set; } = string.Empty;
        public string Status { get; set; } = "Active";
    }
}

