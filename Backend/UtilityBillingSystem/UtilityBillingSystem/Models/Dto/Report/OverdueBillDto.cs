namespace UtilityBillingSystem.Models.Dto.Report
{
    public class OverdueBillDto
    {
        public string BillId { get; set; } = string.Empty;
        public string ConsumerName { get; set; } = string.Empty;
        public string UtilityName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime DueDate { get; set; }
    }
}

