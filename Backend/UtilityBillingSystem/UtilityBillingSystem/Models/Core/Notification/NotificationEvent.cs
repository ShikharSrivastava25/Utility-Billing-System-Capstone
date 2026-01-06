namespace UtilityBillingSystem.Models.Core.Notification
{
    public class NotificationEvent
    {
        public string UserId { get; set; } = string.Empty;
        public string? BillId { get; set; }
        public string Type { get; set; } = string.Empty; 
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime? DueDate { get; set; }
        public int? DaysUntilDue { get; set; }
        public decimal? Amount { get; set; }
        public string? UtilityName { get; set; }
        public string? BillingPeriod { get; set; }
    }
}

