using System.ComponentModel.DataAnnotations;
using UtilityBillingSystem.Models.Core;

namespace UtilityBillingSystem.Models.Core.Notification
{
    public class Notification
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        [Required]
        public string UserId { get; set; } = string.Empty;
        public string? BillId { get; set; }
        [Required]
        [MaxLength(50)]
        public string Type { get; set; } = string.Empty;
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;
        [Required]
        [MaxLength(1000)]
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; } = false;
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public User User { get; set; } = null!;
        public Bill? Bill { get; set; }
    }
}

