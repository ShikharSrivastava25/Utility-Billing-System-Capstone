using System.ComponentModel.DataAnnotations;

namespace UtilityBillingSystem.Models.Core
{
    public class Connection
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        [Required]
        public string UserId { get; set; } = string.Empty;
       
        [Required]
        public string UtilityTypeId { get; set; } = string.Empty;
        
        [Required]
        public string TariffId { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(50)]
        public string MeterNumber { get; set; } = string.Empty; // Unique
        
        [MaxLength(20)]
        public string Status { get; set; } = "Active"; // Active, Inactive

        // Navigation properties
        public User User { get; set; } = null!;
        public UtilityType UtilityType { get; set; } = null!;
        public Tariff Tariff { get; set; } = null!;
        public ICollection<Bill> Bills { get; set; } = new List<Bill>();
    }
}

