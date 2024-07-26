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
                _logger.LogInformation("[x] Fetching news sources for changes.");

                var sources = await dbContext.Sources.AsNoTracking().ToListAsync();
                var newArticles = new ConcurrentBag<Article>();
                var existingTitles = await dbContext.Articles.AsNoTracking()
                    .Select(a => a.Title)
                    .ToListAsync();

                var maxDegreeOfParallelism = 10;
                await Parallel.ForEachAsync(sources, new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism }, async (source, token) =>
                {
                    var items = await RssService.ParseRssFeed(source.Url);
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

                    foreach (var article in articlesToAdd)
                    {
                        newArticles.Add(article);
                    }
                });

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
            await dbContext.DisposeAsync();
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
