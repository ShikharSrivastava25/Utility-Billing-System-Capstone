namespace UtilityBillingSystem.Models.Dto.Payment
{
    public class PaymentDto
    {
        public string Id { get; set; } = string.Empty;
        public string BillId { get; set; } = string.Empty;
        public DateTime PaymentDate { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string? ReceiptNumber { get; set; }
        public string? UpiId { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
