using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UtilityBillingSystem.Models.Dto;
using UtilityBillingSystem.Models.Dto.Notification;
using UtilityBillingSystem.Services.Interfaces;

namespace UtilityBillingSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Consumer")]
    public class NotificationController : BaseController
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<NotificationDto>>> GetNotifications([FromQuery] bool? unreadOnly = null)
        {
            var notifications = await _notificationService.GetUserNotificationsAsync(CurrentUserId, unreadOnly);
            return Ok(notifications);
        }

        [HttpGet("unread-count")]
        public async Task<ActionResult<int>> GetUnreadCount()
        {
            var count = await _notificationService.GetUnreadCountAsync(CurrentUserId);
            return Ok(count);
        }

        [HttpPut("{id}/read")]
        public async Task<ActionResult<MessageResponseDto>> MarkAsRead(string id)
        {
            await _notificationService.MarkAsReadAsync(id, CurrentUserId);
            return Ok(new MessageResponseDto { Message = "Notification marked as read" });
        }

        [HttpPut("mark-all-read")]
        public async Task<ActionResult<MessageResponseDto>> MarkAllAsRead()
        {
            await _notificationService.MarkAllAsReadAsync(CurrentUserId);
            return Ok(new MessageResponseDto { Message = "All notifications marked as read" });
        }

        [HttpDelete("all")]
        public async Task<ActionResult<MessageResponseDto>> DeleteAllNotifications()
        {
            await _notificationService.DeleteAllNotificationsAsync(CurrentUserId);
            return Ok(new MessageResponseDto { Message = "All notifications deleted" });
        }
    }
}

