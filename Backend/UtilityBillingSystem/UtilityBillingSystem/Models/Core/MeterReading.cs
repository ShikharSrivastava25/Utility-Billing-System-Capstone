using System.ComponentModel.DataAnnotations;

namespace UtilityBillingSystem.Models.Core
{
    public class MeterReading
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        [Required]
        public string ConnectionId { get; set; } = string.Empty;
        
        [Range(0, double.MaxValue, ErrorMessage = "Previous reading must be non-negative")]
        public decimal PreviousReading { get; set; }
        
        [Range(0, double.MaxValue, ErrorMessage = "Current reading must be non-negative")]
        public decimal CurrentReading { get; set; }
        
        [Range(0, double.MaxValue, ErrorMessage = "Consumption must be non-negative")]
        public decimal Consumption { get; set; } // Computed: CurrentReading - PreviousReading
        
        [Required]
        public DateTime ReadingDate { get; set; }
        
        [MaxLength(20)]
        public string Status { get; set; } = "ReadyForBilling"; // ReadyForBilling, Billed
        
        [Required]
        [MaxLength(200)]
        public string RecordedBy { get; set; } = string.Empty; // User email or name
        public string? BillingCycleId { get; set; }
        
        [Required]
        public string TariffId { get; set; } = string.Empty; // Store tariff at time of reading for historical accuracy
        
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Connection Connection { get; set; } = null!;
        public BillingCycle? BillingCycle { get; set; }
        public Tariff Tariff { get; set; } = null!;
    }
}

