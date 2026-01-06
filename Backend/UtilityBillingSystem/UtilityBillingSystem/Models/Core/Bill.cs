using System.ComponentModel.DataAnnotations;

namespace UtilityBillingSystem.Models.Core
{
    public class Bill
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        [Required]
        public string ConnectionId { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(50)]
        public string BillingPeriod { get; set; } = string.Empty; // e.g., "July 2024"
        
        [Required]
        public DateTime GenerationDate { get; set; } = DateTime.UtcNow;
        public DateTime DueDate { get; set; }
        
        [Range(0, double.MaxValue, ErrorMessage = "Previous reading must be non-negative")]
        public decimal PreviousReading { get; set; }
        
        [Range(0, double.MaxValue, ErrorMessage = "Current reading must be non-negative")]
        public decimal CurrentReading { get; set; }
        
        [Range(0, double.MaxValue, ErrorMessage = "Consumption must be non-negative")]
        public decimal Consumption { get; set; }
        
        [Range(0, double.MaxValue, ErrorMessage = "Base amount must be non-negative")]
        public decimal BaseAmount { get; set; }
        
        [Range(0, double.MaxValue, ErrorMessage = "Tax amount must be non-negative")]
        public decimal TaxAmount { get; set; }
        
        [Range(0, double.MaxValue, ErrorMessage = "Penalty amount must be non-negative")]
        public decimal PenaltyAmount { get; set; } = 0;
        
        [Range(0, double.MaxValue, ErrorMessage = "Total amount must be non-negative")]
        public decimal TotalAmount { get; set; }
        
        [MaxLength(20)]
        public string Status { get; set; } = "Generated"; // Generated, Due, Paid, Overdue

        // Navigation properties
        public Connection Connection { get; set; } = null!;
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}

