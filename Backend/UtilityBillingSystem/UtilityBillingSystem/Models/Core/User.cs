using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace UtilityBillingSystem.Models.Core
{
    public class User : IdentityUser
    {
        [Required]
        [MaxLength(200)]
        public string FullName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        
        [MaxLength(20)]
        public string Status { get; set; } = "Active"; // Active, Inactive, Deleted
        
        public DateTime? DeletedAt { get; set; }

        // Navigation properties
        public ICollection<Connection> Connections { get; set; } = new List<Connection>();
        public ICollection<UtilityRequest> UtilityRequests { get; set; } = new List<UtilityRequest>();
    }
}

