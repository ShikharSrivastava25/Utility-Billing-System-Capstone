using System.Threading.Channels;

namespace UtilityBillingSystem.Models.Core.Notification
{
    public static class NotificationQueue
    {
        public static Channel<NotificationEvent> Channel = 
            System.Threading.Channels.Channel.CreateUnbounded<NotificationEvent>();
    }
}

