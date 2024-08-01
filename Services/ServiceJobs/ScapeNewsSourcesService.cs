using System.Net.Sockets;
using AutoMapper;
using News_aggregation.Entities;
using NewsAggregation.Data.UnitOfWork;

namespace NewsAggregation.Services.ServiceJobs
{
    using Microsoft.AspNetCore.SignalR;
    using Microsoft.EntityFrameworkCore;
    using Nest;
    using NewsAggregation.Data;
    using NewsAggregation.DTO.Article;
    using NewsAggregation.Services.ServiceJobs;
    using Polly;
    using Polly.Retry;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Policy = Polly.Policy;

    public class ScapeNewsSourcesService : BackgroundService
    {
        private readonly ILogger<ScapeNewsSourcesService> _logger;
        private Timer _timer;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly AsyncRetryPolicy _retryPolicy;
        private readonly IElasticClient _elasticClient;

        public ScapeNewsSourcesService(ILogger<ScapeNewsSourcesService> logger, IServiceScopeFactory serviceScopeFactory, IElasticClient elasticClient)
        {
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
            _elasticClient = elasticClient;

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

        /*
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

                                    var duplicateTitles = allTitles
                                        .GroupBy(x => x)
                                        .Where(g => g.Count() > 1)
                                        .Select(y => y.Key)
                                        .ToList();

                                    if (duplicateTitles.Any())
                                    {
                                        // Remove duplicate titles
                                        allItems = allItems.Where(i => !duplicateTitles.Contains(i.Title)).ToList();
                                    }

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
                                                PublishedAt = DateTime.Parse(item.PubDate).ToUniversalTime(),
                                                Tags = string.Join(",", (item.Title ?? string.Empty).Split(' ')
                                                    .GroupBy(x => x)
                                                    .OrderByDescending(x => x.Count())
                                                    .Select(x => x.Key)
                                                    .Take(item.Title.Split(' ').Length)
                                                    .Where(tag => !string.IsNullOrWhiteSpace(tag))),
                                                CreatedAt = DateTime.UtcNow,
                                                UpdatedAt = DateTime.UtcNow,
                                                Content = item.Content ?? "No Content"
                                            })
                                            .ToList();

                                        newArticles.AddRange(articlesToAdd);

                                        // Index articles in Elasticsearch
                                        var indexTasks = articlesToAdd.Select(article =>
                                                                               _elasticClient.IndexDocumentAsync(article)
                                                                                                                  ).ToList();

                                        await Task.WhenAll(indexTasks);

                                        _logger.LogInformation($"{articlesToAdd.Count} articles indexed in Elasticsearch.");

                                    }

                                    // Batch index articles in Elasticsearch
                                    if (newArticles.Any())
                                    {
                                        var bulkIndexResponse = await _elasticClient.BulkAsync(b => b
                                            .Index("articles")
                                            .IndexMany(newArticles)
                                        );

                                        if (bulkIndexResponse.Errors)
                                        {
                                            _logger.LogError("Errors occurred during bulk indexing.");
                                            foreach (var itemWithError in bulkIndexResponse.ItemsWithErrors)
                                            {
                                                _logger.LogError($"Failed to index document {itemWithError.Id}: {itemWithError.Error.Reason}");
                                            }
                                        }
                                        else
                                        {
                                            _logger.LogInformation($"{newArticles.Count} articles indexed in Elasticsearch.");
                                        }

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
        */

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

                        var duplicateTitles = allTitles
                            .GroupBy(x => x)
                            .Where(g => g.Count() > 1)
                            .Select(y => y.Key)
                            .ToList();

                        if (duplicateTitles.Any())
                        {
                            // Remove duplicate titles
                            allItems = allItems.Where(i => !duplicateTitles.Contains(i.Title)).ToList();
                        }

                        // Filter out existing articles and prepare new articles list
                        foreach (var result in results)
                        {
                            var source = result.Source;
                            var items = result.Items;

                            var articlesToAdd = items
                                .Where(item => !existingTitles.Contains(item.Title))
                                .Select(item => new Article
                                {
                                    Id = Guid.NewGuid(), // Ensure each article has a unique ID
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
                                    PublishedAt = DateTime.Parse(item.PubDate).ToUniversalTime(),
                                    Tags = string.Join(",", (item.Title ?? string.Empty).Split(' ')
                                        .GroupBy(x => x)
                                        .OrderByDescending(x => x.Count())
                                        .Select(x => x.Key)
                                        .Take(item.Title.Split(' ').Length)
                                        .Where(tag => !string.IsNullOrWhiteSpace(tag))),
                                    CreatedAt = DateTime.UtcNow,
                                    UpdatedAt = DateTime.UtcNow,
                                    Content = item.Content ?? "No Content"
                                })
                                .ToList();

                            newArticles.AddRange(articlesToAdd);
                        }

                        // Log the number of articles to be indexed
                        _logger.LogInformation($"Preparing to index {newArticles.Count} articles.");

                        // Batch index articles in Elasticsearch with unique IDs
                        if (newArticles.Any())
                        {
                            var bulkIndexResponse = await _elasticClient.BulkAsync(b => b
                                .Index("articles")
                                .IndexMany(newArticles, (descriptor, article) => descriptor.Id(article.Id))
                            );

                            if (bulkIndexResponse.Errors)
                            {
                                _logger.LogError("Errors occurred during bulk indexing.");
                                foreach (var itemWithError in bulkIndexResponse.ItemsWithErrors)
                                {
                                    _logger.LogError($"Failed to index document {itemWithError.Id}: {itemWithError.Error.Reason}");
                                }
                            }
                            else
                            {
                                _logger.LogInformation($"{newArticles.Count} articles indexed in Elasticsearch.");
                            }

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

            _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromMinutes(2));

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

    }
}
