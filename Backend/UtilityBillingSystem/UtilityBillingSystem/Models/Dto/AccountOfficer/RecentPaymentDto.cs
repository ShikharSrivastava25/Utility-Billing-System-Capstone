namespace UtilityBillingSystem.Models.Dto.AccountOfficer
{
    public class RecentPaymentDto
    {
        public DateTime Date { get; set; }
        public string ConsumerName { get; set; } = string.Empty;
        public string UtilityName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Method { get; set; } = string.Empty;
    }
}

