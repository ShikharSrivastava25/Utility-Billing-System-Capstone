namespace UtilityBillingSystem.Models.Dto.AccountOfficer
{
    public class ConsumerBillingSummaryDto
    {
        public string ConsumerId { get; set; } = string.Empty;
        public string ConsumerName { get; set; } = string.Empty;
        public decimal TotalBilled { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal OutstandingBalance { get; set; }
        public int OverdueCount { get; set; }
    }
}

