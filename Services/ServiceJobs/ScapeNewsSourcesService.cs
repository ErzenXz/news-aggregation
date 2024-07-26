using System.Net.Sockets;
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
    using Polly;
    using Polly.Retry;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    public class ScapeNewsSourcesService : BackgroundService
    {
        private readonly ILogger<ScapeNewsSourcesService> _logger;
        private Timer _timer;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly AsyncRetryPolicy _retryPolicy;

        public ScapeNewsSourcesService(ILogger<ScapeNewsSourcesService> logger, IServiceScopeFactory serviceScopeFactory)
        {
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;

            // Configure Polly retry policy
            _retryPolicy = Policy
                .Handle<HttpRequestException>()
                .Or<SocketException>()
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (exception, timeSpan, retryCount, context) =>
                    {
                        _logger.LogWarning($"Retry {retryCount} encountered an error: {exception.Message}. Waiting {timeSpan} before next retry.");
                    });

        }

        private async void DoWork(object state)
        {
            _logger.LogInformation("[x] Fetching news sources.");
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<DBContext>();

                try
                {
                    await _retryPolicy.ExecuteAsync(async () =>
                            {
                                // Get all sources from the database
                                var sources = await dbContext.Sources.AsNoTracking().ToListAsync();

                                // Fetch the RSS feeds for all sources in parallel
                                var fetchTasks = sources.Select(source =>
                                    RssService.ParseRssFeed(source.Url)
                                        .ContinueWith(t => new { Source = source, Items = t.Result })
                                ).ToList();

                                var results = await Task.WhenAll(fetchTasks);

                                var newArticles = new List<Article>();

                                // Combine all titles from the items
                                var allItems = results.SelectMany(r => r.Items).ToList();
                                var allTitles = allItems.Select(i => i.Title).ToList();

                                // Batch check if the articles already exist in the database
                                var existingTitles = await dbContext.Articles.AsNoTracking()
                                    .Where(a => allTitles.Contains(a.Title))
                                    .Select(a => a.Title)
                                    .ToListAsync();

                                // Filter out existing articles and prepare new articles list
                                foreach (var result in results)
                                {
                                    var source = result.Source;
                                    var items = result.Items;

                                    var articlesToAdd = items
                                        .Where(item => !existingTitles.Contains(item.Title))
                                        .Select(item => new Article
                                        {
                                            Title = item.Title ?? "No Title",
                                            Description = item.Description ?? "No Description",
                                            ImageUrl = item.Image ?? "https://via.placeholder.com/500",
                                            Url = item.Link ?? "https://example.com",
                                            SourceId = source.Id,
                                            AuthorId = new Guid("46611d66-cbd8-4a2f-9f9c-527edeab3984"),
                                            CategoryId = 1,
                                            Views = 0,
                                            Likes = 0,
                                            IsPublished = true,
                                            PublishedAt = DateTime.UtcNow,
                                            Tags = string.Join(",", (item.Description ?? string.Empty).Split(' ')
                                                .GroupBy(x => x)
                                                .OrderByDescending(x => x.Count())
                                                .Select(x => x.Key)
                                                .Take(10)
                                                .Where(tag => !string.IsNullOrWhiteSpace(tag))),
                                            CreatedAt = DateTime.UtcNow,
                                            UpdatedAt = DateTime.UtcNow,
                                            Content = item.Content ?? "No Content"
                                        })
                                        .ToList();

                                    newArticles.AddRange(articlesToAdd);
                                }

                                if (newArticles.Any())
                                {
                                    await dbContext.Articles.AddRangeAsync(newArticles);
                                    await dbContext.SaveChangesAsync();
                                    _logger.LogInformation($"{newArticles.Count} articles added to the database.");
                                }
                            });

                }
                catch (Exception ex)
                {
                    _logger.LogError($"An error occurred while processing sources: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        _logger.LogError($"Inner exception: {ex.InnerException.Message}");
                    }
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


        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("[x] Background News Scrapper Service is starting.");

            _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromMinutes(7));

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

    }
}
