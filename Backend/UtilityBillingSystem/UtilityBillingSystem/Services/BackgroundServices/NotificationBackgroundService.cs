using Microsoft.EntityFrameworkCore;
using UtilityBillingSystem.Data;
using UtilityBillingSystem.Models.Core.Notification;

namespace UtilityBillingSystem.Services.BackgroundServices
{
    public class NotificationBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public NotificationBackgroundService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine("NotificationBackgroundService STARTED");

            // Start due date checker timer (runs every hour)
            _ = Task.Run(() => CheckDueDatesPeriodically(stoppingToken), stoppingToken);

            try
            {
                await foreach (var evt in NotificationQueue.Channel.Reader.ReadAllAsync(stoppingToken))
                {
                    await ProcessNotificationEvent(evt);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Background Service Error");
                Console.WriteLine(ex.ToString());
            }
        }

        private async Task ProcessNotificationEvent(NotificationEvent evt)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var notification = new Notification
                {
                    UserId = evt.UserId,
                    BillId = evt.BillId,
                    Type = evt.Type,
                    Title = evt.Title,
                    Message = evt.Message,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };

                db.Notifications.Add(notification);
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Keep error logging for debugging
                Console.WriteLine($"Error processing notification event: {ex.Message}");
                Console.WriteLine(ex.ToString());
            }
        }

        private async Task CheckDueDatesPeriodically(CancellationToken stoppingToken)
        {
            var reminderDays = new[] { 7, 3, 0 }; // 7 days before, 3 days before, on due date

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var now = DateTime.UtcNow.Date;

                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    foreach (var days in reminderDays)
                    {
                        var targetDate = now.AddDays(days);

                        // Find bills due on target date that are not paid or overdue
                        var bills = await db.Bills
                            .Include(b => b.Connection)
                                .ThenInclude(c => c.User)
                            .Include(b => b.Connection)
                                .ThenInclude(c => c.UtilityType)
                            .Where(b => b.DueDate.Date == targetDate &&
                                       (b.Status == "Generated" || b.Status == "Due"))
                            .ToListAsync(stoppingToken);

                        Console.WriteLine($"[REMINDER CHECK] Found {bills.Count} bills due in {days} days (Date: {targetDate:yyyy-MM-dd})");

                        foreach (var bill in bills)
                        {
                            // Check if reminder already sent today for this bill and reminder type
                            var reminderType = $"DueDateReminder_{days}Days";
                            var alreadySent = await db.Notifications
                                .AnyAsync(n => n.BillId == bill.Id &&
                                              n.Type == "DueDateReminder" &&
                                              n.Message.Contains($"{days} days") &&
                                              n.CreatedAt.Date == now, stoppingToken);

                            if (alreadySent)
                                continue;

                            if (bill.Connection?.User == null || bill.Connection.UtilityType == null)
                                continue;

                            string message;
                            string title;

                            if (days == 0)
                            {
                                title = "Urgent: Bill Due Today";
                                message = $"Urgent: Your {bill.Connection.UtilityType.Name} bill of ₹{bill.TotalAmount:N2} is due today ({bill.DueDate:dd MMM yyyy})";
                            }
                            else
                            {
                                title = $"Reminder: Bill Due in {days} Days";
                                message = $"Reminder: Your {bill.Connection.UtilityType.Name} bill of ₹{bill.TotalAmount:N2} is due in {days} days (Due: {bill.DueDate:dd MMM yyyy})";
                            }

                            var reminderEvent = new NotificationEvent
                            {
                                UserId = bill.Connection.UserId,
                                BillId = bill.Id,
                                Type = "DueDateReminder",
                                Title = title,
                                Message = message,
                                DueDate = bill.DueDate,
                                DaysUntilDue = days,
                                Amount = bill.TotalAmount,
                                UtilityName = bill.Connection.UtilityType.Name,
                                BillingPeriod = bill.BillingPeriod
                            };

                            await NotificationQueue.Channel.Writer.WriteAsync(reminderEvent, stoppingToken);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error checking due dates: {ex.Message}");
                    Console.WriteLine(ex.ToString());
                }

                // Wait 1 hour before next check
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }
    }
}

