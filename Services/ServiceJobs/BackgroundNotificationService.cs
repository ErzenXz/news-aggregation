using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NewsAggregation.Data;
using NewsAggregation.Services.ServiceJobs.Hubs;

public class BackgroundNotificationService : BackgroundService
{
    private readonly ILogger<BackgroundNotificationService> _logger;
    private Timer _timer;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IHubContext<NotificationHub> _hubContext;

    public BackgroundNotificationService(ILogger<BackgroundNotificationService> logger, IServiceScopeFactory serviceScopeFactory, IHubContext<NotificationHub> hubContext)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
        _hubContext = hubContext;
    }

    private async void DoWork(object state)
    {
        using (var scope = _serviceScopeFactory.CreateScope())
        {
            _logger.LogInformation("[x] BG-NService is working.");
            var dbContext = scope.ServiceProvider.GetRequiredService<DBContext>();

            try
            {
                var notifications = await dbContext.Notifications.Where(n => !n.IsRead).AsNoTracking().ToListAsync();

                foreach (var notification in notifications)
                {
                    // Send notification via SignalR
                    await _hubContext.Clients.All.SendAsync("ReceiveNotification", notification);
                    // Log the notification
                    _logger.LogInformation($"[x] Notification sent: {notification.Content}");

                    // Mark notification as read
                    notification.IsRead = true;
                }

                await dbContext.SaveChangesAsync();
                _logger.LogInformation("[x] Notifications processed and saved.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[x] An error occurred while processing notifications.");
            }
            finally
            {
                if (dbContext != null)
                {
                    await dbContext.DisposeAsync();
                }
            }
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[x] Background Notification Service is starting.");

        _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromSeconds(30));

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

}
