using System.ComponentModel.DataAnnotations;

namespace UtilityBillingSystem.Models.Core
{
    public class Payment
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        [Required]
        public string BillId { get; set; } = string.Empty;
        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
        
        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Amount must be non-negative")]
        public decimal Amount { get; set; }
        
        [Required]
        [MaxLength(20)]
        public string PaymentMethod { get; set; } = string.Empty; // Cash, Online
        public string? ReceiptNumber { get; set; }
        public string? UpiId { get; set; }
        
        [MaxLength(20)]
        public string Status { get; set; } = "Completed"; // For future extension

        // Navigation
        public Bill Bill { get; set; } = null!;
    }
}

