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

                    // Store the items in the database
                    foreach (var item in items)
                    {
                        // Check if the article already exists in the database
                        var existingArticle = await dbContext.Articles.FirstOrDefaultAsync(x => x.Title == item.Title);
                        if (existingArticle != null)
                        {
                            continue;
                        }

                        var article = new Article();
                        article.Title = item.Title;
                        article.Description = item.Description;

                        if (item.Image != null)
                        {
                            article.ImageUrl = item.Image;
                        } else
                        {
                            article.ImageUrl = "https://via.placeholder.com/150";
                        }

                        article.Url = item.Link;
                        article.SourceId = source.Id;
                        article.AuthorId = new Guid("46611d66-cbd8-4a2f-9f9c-527edeab3984");

                        article.CategoryId = 1;
                        article.Views = 0;
                        article.Likes = 0;
                        article.IsPublished = true;
                        var pubDateString = item.PubDate;
                        if (DateTime.TryParse(pubDateString, out DateTime pubDate))
                        {
                            article.PublishedAt = pubDate;
                        }
                        
                        // Create tags for the article by selecting the most used words in the description without an api
                        var tags = item.Description.Split(' ')
                            .GroupBy(x => x)
                            .OrderByDescending(x => x.Count())
                            .Select(x => x.Key)
                            .Take(5)
                            .ToList();

                        var tagsString = "";

                        foreach (var tag in tags)
                        {
                            tagsString += tag + ",";
                        }

                        article.Tags = tagsString;
                        article.CreatedAt = DateTime.UtcNow;
                        article.UpdatedAt = DateTime.UtcNow;
                        article.Content = item.Description;

                        // Save the article to the database

                        var result = await dbContext.Articles.AddAsync(article);
                        if (result.State == EntityState.Added)
                        {
                            _logger.LogInformation($"Article {article.Title} added to the database.");
                        }
                    }

                    await dbContext.SaveChangesAsync();

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
