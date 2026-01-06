namespace UtilityBillingSystem.Models.Dto.Payment
{
    public class PaymentHistoryDto
    {
        public string Id { get; set; } = string.Empty;
        public DateTime PaymentDate { get; set; }
        public string BillId { get; set; } = string.Empty;
        public string BillingPeriod { get; set; } = string.Empty;
        public string UtilityName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}

