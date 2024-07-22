using AutoMapper;
using News_aggregation.Entities;
using NewsAggregation.Data.UnitOfWork;

namespace NewsAggregation.Services.ServiceJobs
{
    using Microsoft.AspNetCore.SignalR;
    using Microsoft.EntityFrameworkCore;
    using NewsAggregation.Data;
    using NewsAggregation.DTO.Article;
    using NewsAggregation.Services.ServiceJobs;

    public class ScapeNewsSourcesService : BackgroundService
    {
        private readonly ILogger<BackgroundNotificationService> _logger;
        private Timer _timer;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;


        public ScapeNewsSourcesService(ILogger<BackgroundNotificationService> logger, IServiceScopeFactory serviceScopeFactory)
        {
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;

        }

        private async void DoWork(object state)
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<DBContext>();
                // Get all sources from the database
                var sources = await dbContext.Sources.ToListAsync();

                foreach (var source in sources)
                {
                    // Fetch the RSS feed from the source
                    var items = await RssService.ParseRssFeed(source.Url);

                    // If the source has no items, skip it
                    if (items.Count == 0)
                    {
                        continue;
                    }

                    var articlesToAdd = new List<Article>();

                    foreach (var item in items)
                    {
                        // Check if the article already exists in the database
                        var existingArticle = await dbContext.Articles.FirstOrDefaultAsync(x => x.Title == item.Title);
                        if (existingArticle != null)
                        {
                            continue;
                        }

                        var article = new Article
                        {
                            Title = item.Title,
                            Description = item.Description,
                            ImageUrl = item.Image ?? "https://via.placeholder.com/150",
                            Url = item.Link,
                            SourceId = source.Id,
                            AuthorId = new Guid("46611d66-cbd8-4a2f-9f9c-527edeab3984"),
                            CategoryId = 1,
                            Views = 0,
                            Likes = 0,
                            IsPublished = true,
                            PublishedAt = DateTime.UtcNow,
                            Tags = string.Join(",", item.Description.Split(' ')
                                .GroupBy(x => x)
                                .OrderByDescending(x => x.Count())
                                .Select(x => x.Key)
                                .Take(10)
                                .Where(tag => !string.IsNullOrWhiteSpace(tag))),
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow,
                            Content = item.Description
                        };

                        if (item.Image != null)
                        {
                            article.ImageUrl = item.Image;
                        }
                        else
                        {
                            article.ImageUrl = "https://via.placeholder.com/150";
                        }

                        articlesToAdd.Add(article);
                    }

                    if (articlesToAdd.Any())
                    {
                        await dbContext.Articles.AddRangeAsync(articlesToAdd);
                        await dbContext.SaveChangesAsync();
                        _logger.LogInformation($"{articlesToAdd.Count} articles added to the database from source {source.Url}.");
                    }
                }
            }
        }



        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("[x] Background News Scrapper Service is starting.");

            _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromMinutes(3));

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

    }
}
