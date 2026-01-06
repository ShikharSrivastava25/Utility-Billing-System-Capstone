using System.ComponentModel.DataAnnotations;

namespace UtilityBillingSystem.Models.Core
{
    public class UtilityType
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;
        
        [MaxLength(20)]
        public string Status { get; set; } = "Enabled"; // Enabled, Disabled
        public string? BillingCycleId { get; set; }

        // Navigation properties
        public BillingCycle? BillingCycle { get; set; }
        public ICollection<Tariff> Tariffs { get; set; } = new List<Tariff>();
        public ICollection<Connection> Connections { get; set; } = new List<Connection>();
        public ICollection<UtilityRequest> UtilityRequests { get; set; } = new List<UtilityRequest>();
    }
}

