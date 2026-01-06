namespace UtilityBillingSystem.Models.Dto.Payment
{
    public class CreatePaymentDto
    {
        public string PaymentMethod { get; set; } = string.Empty; // Cash, Online
        public string? ReceiptNumber { get; set; }
        public string? UpiId { get; set; }
    }
}

