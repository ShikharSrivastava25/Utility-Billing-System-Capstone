using System.ComponentModel.DataAnnotations;

namespace UtilityBillingSystem.Models.Core
{
    public class UtilityRequest
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        [Required]
        public string UserId { get; set; } = string.Empty;
        
        [Required]
        public string UtilityTypeId { get; set; } = string.Empty;
        
        [MaxLength(20)]
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected
        
        [Required]
        public DateTime RequestDate { get; set; } = DateTime.UtcNow;
        public DateTime? DecisionDate { get; set; }

        // Navigation properties
        public User User { get; set; } = null!;
        public UtilityType UtilityType { get; set; } = null!;
    }
}

