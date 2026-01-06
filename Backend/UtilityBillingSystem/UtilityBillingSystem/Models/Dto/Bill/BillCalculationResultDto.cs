namespace UtilityBillingSystem.Models.Dto.Bill
{
    public class BillCalculationResultDto
    {
        public decimal Consumption { get; set; }
        public decimal BaseRate { get; set; }
        public decimal FixedCharge { get; set; }
        public decimal BaseAmount { get; set; }
        public decimal TaxPercentage { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
    }
}

