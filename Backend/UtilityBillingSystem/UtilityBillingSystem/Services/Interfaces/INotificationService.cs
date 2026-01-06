using UtilityBillingSystem.Models.Dto.Notification;

namespace UtilityBillingSystem.Services.Interfaces
{
    public interface INotificationService
    {
        Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync(string userId, bool? unreadOnly = null);
        Task<int> GetUnreadCountAsync(string userId);
        Task MarkAsReadAsync(string notificationId, string userId);
        Task MarkAllAsReadAsync(string userId);
        Task DeleteAllNotificationsAsync(string userId);
    }
}

