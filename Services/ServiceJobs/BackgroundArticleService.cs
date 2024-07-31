namespace NewsAggregation.Services.ServiceJobs
{
    using Microsoft.AspNetCore.SignalR;
    using Microsoft.EntityFrameworkCore;
    using NewsAggregation.Data;
    using NewsAggregation.Services.ServiceJobs.Hubs;

    public class BackgroundArticleService : BackgroundService
    {
        private readonly ILogger<BackgroundArticleService> _logger;
        private Timer _timer;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IHubContext<NewsHub> _hubContext;

        public BackgroundArticleService(ILogger<BackgroundArticleService> logger, IServiceScopeFactory serviceScopeFactory, IHubContext<NewsHub> hubContext)
        {
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
            _hubContext = hubContext;
        }

        private async void DoWork(object state)
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                _logger.LogInformation("[x] BG-ArticleService is working. :D");
                var dbContext = scope.ServiceProvider.GetRequiredService<DBContext>();

                try
                {
                    var latestArticles = await dbContext.Articles
                        .OrderByDescending(a => a.CreatedAt)
                        .Where(a => a.CreatedAt > DateTime.UtcNow.AddMinutes(-1))
                        .Take(10)
                        .AsNoTracking()
                        .ToListAsync();

                    foreach (var article in latestArticles)
                    {
                        // Send article via SignalR
                        await _hubContext.Clients.All.SendAsync("ReceiveArticle", article);
                        // Log the article
                        _logger.LogInformation($"[x] Article sent: {article.Title}");
                    }

                    _logger.LogInformation("[x] Articles processed and sent.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[x] An error occurred while processing articles.");
                }
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("[x] Background Article Service is starting.");

            _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

    }

}
