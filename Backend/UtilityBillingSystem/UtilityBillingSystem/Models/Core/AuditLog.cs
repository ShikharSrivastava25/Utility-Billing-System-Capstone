using System.ComponentModel.DataAnnotations;

namespace UtilityBillingSystem.Models.Core
{
    public class AuditLog
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        [Required]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [Required]
        [MaxLength(100)]
        public string Action { get; set; } = string.Empty; // e.g., "USER_CREATE", "TARIFF_UPDATE"
        
        [MaxLength(1000)]
        public string Details { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(200)]
        public string PerformedBy { get; set; } = string.Empty; 
    }
}

