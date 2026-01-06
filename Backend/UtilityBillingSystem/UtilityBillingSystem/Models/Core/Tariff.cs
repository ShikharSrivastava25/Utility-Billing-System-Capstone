using System.ComponentModel.DataAnnotations;

namespace UtilityBillingSystem.Models.Core
{
    public class Tariff
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        public string UtilityTypeId { get; set; } = string.Empty;
        
        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Base rate must be non-negative")]
        public decimal BaseRate { get; set; } // Rate per unit
        
        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Fixed charge must be non-negative")]
        public decimal FixedCharge { get; set; } // Monthly fixed charge
        
        [Required]
        [Range(0, 100, ErrorMessage = "Tax percentage must be between 0 and 100")]
        public decimal TaxPercentage { get; set; } // Tax percentage
        
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;

        // Navigation properties
        public UtilityType UtilityType { get; set; } = null!;
        public ICollection<Connection> Connections { get; set; } = new List<Connection>();
    }
}

