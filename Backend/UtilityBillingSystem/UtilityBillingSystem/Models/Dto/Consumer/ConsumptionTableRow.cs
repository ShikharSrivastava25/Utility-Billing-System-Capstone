namespace UtilityBillingSystem.Models.Dto.Consumer
{
    public class ConsumptionTableRow
    {
        public string Month { get; set; } = string.Empty;
        public decimal Units { get; set; }
        public decimal EstimatedCost { get; set; }
    }
}

