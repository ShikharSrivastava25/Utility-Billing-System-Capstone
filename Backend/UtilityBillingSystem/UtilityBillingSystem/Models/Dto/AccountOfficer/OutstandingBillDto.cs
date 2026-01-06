namespace UtilityBillingSystem.Models.Dto.AccountOfficer
{
    public class OutstandingBillDto
    {
        public string BillId { get; set; } = string.Empty;
        public string ConsumerId { get; set; } = string.Empty;
        public string ConsumerName { get; set; } = string.Empty;
        public string UtilityName { get; set; } = string.Empty;
        public string BillMonth { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty; // "Due" or "Overdue"
        public DateTime DueDate { get; set; }
    }
}

