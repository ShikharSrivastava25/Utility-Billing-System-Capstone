using System.ComponentModel.DataAnnotations;

namespace UtilityBillingSystem.Models.Core
{
    public class BillingCycle
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        [Range(1, 28, ErrorMessage = "Generation day must be between 1 and 28")]
        public int GenerationDay { get; set; } // Day of month (1-28)
        public int DueDateOffset { get; set; } // Days after generation date
        public int GracePeriod { get; set; } // Days after due date
        public bool IsActive { get; set; } = true;

        // Navigation properties
        public ICollection<UtilityType> UtilityTypes { get; set; } = new List<UtilityType>();
    }
}

