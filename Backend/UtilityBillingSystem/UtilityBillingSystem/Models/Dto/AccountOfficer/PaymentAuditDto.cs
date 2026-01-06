namespace UtilityBillingSystem.Models.Dto.AccountOfficer
{
    public class PaymentAuditDto
    {
        public DateTime Date { get; set; }
        public string ConsumerId { get; set; } = string.Empty;
        public string ConsumerName { get; set; } = string.Empty;
        public string UtilityName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Method { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
    }
}

