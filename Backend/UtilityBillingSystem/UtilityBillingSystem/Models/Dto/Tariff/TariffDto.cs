namespace UtilityBillingSystem.Models.Dto.Tariff
{
    public class TariffDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string UtilityTypeId { get; set; } = string.Empty;
        public decimal BaseRate { get; set; }
        public decimal FixedCharge { get; set; }
        public decimal TaxPercentage { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
    }
}

