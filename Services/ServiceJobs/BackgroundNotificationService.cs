using Microsoft.EntityFrameworkCore;
using NewsAggregation.Data;

public class BackgroundNotificationService : BackgroundService
{
    private readonly ILogger<BackgroundNotificationService> _logger;
    private Timer _timer;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public BackgroundNotificationService(ILogger<BackgroundNotificationService> logger, IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
    }

    private async void DoWork(object state)
    {
        using (var scope = _serviceScopeFactory.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<DBContext>();
            var users = await dbContext.Users.ToListAsync();
            foreach (var user in users)
            {
                _logger.LogInformation($"User: {user.Username}");
            }
        }
    }


    protected async override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[x] Background Notification Service is starting.");

        _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromSeconds(10));

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}