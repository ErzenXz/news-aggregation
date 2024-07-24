using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NewsAggregation.Data;
using NewsAggregation.Services.ServiceJobs;

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
            var notifications = await dbContext.Notifications.Where(n => !n.IsRead).ToListAsync();

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
        }
    }


    protected async override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[x] Background Notification Service is starting.");

        _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(500));

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}