namespace UtilityBillingSystem.Models.Dto.Notification
{
    public class CreateNotificationDto
    {
        public string UserId { get; set; } = string.Empty;
        public string? BillId { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}

