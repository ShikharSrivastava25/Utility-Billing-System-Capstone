namespace UtilityBillingSystem.Models.Dto.AuditLog
{
    public class AuditLogDto
    {
        public DateTime Timestamp { get; set; }
        public string Action { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public string PerformedBy { get; set; } = string.Empty;
    }
}

