﻿using AutoMapper;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using News_aggregation.Entities;
using NewsAggregation.Data.UnitOfWork;
using NewsAggregation.DTO.Article;
using NewsAggregation.Helpers;
using NewsAggregation.Services.Interfaces;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using NewsAggregation.Models;
using NewsAggregation.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NewsAggregation.Models.Stats;
using Nest;

namespace NewsAggregation.Services
{
    public class ArticleService : IArticleService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<AuthService> _logger;
        private readonly DBContext _dBContext;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IElasticClient _elasticClient;

        public ArticleService(IMapper mapper, IUnitOfWork unitOfWork, ILogger<AuthService> logger, DBContext dbContext, IHttpContextAccessor httpContextAccessor, IElasticClient elasticClient)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _dBContext = dbContext;
            _httpContextAccessor = httpContextAccessor;
            _elasticClient = elasticClient;
        }


        public async Task<IActionResult> CreateArticle(ArticleCreateDto article)
        {
            try
            {
                var createArticle = _mapper.Map<Article>(article);

                _unitOfWork.Repository<Article>().Create(createArticle);

                await _unitOfWork.CompleteAsync();

                var createdArticleDto = _mapper.Map<ArticleCreateDto>(createArticle);
                return new OkObjectResult(createdArticleDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Creating Article");
                return new StatusCodeResult(500);

            }
        }

        public async Task<IActionResult> DeleteArticle(Guid id)
        {
            try
            {
                var articleToDelete = await _unitOfWork.Repository<Article>().GetById(id);

                if (articleToDelete != null)
                {
                    _unitOfWork.Repository<Article>().Delete(articleToDelete);
                    await _unitOfWork.CompleteAsync();

                    // Remove article from elastic search
                    await _elasticClient.DeleteAsync<Article>(id);

                    return new OkResult();
                }
                else
                {
                    _logger.LogWarning($"Article with id {id} not found.");
                    return new NotFoundResult();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Deleting Article");
                return new StatusCodeResult(500);
            }
        }

        public async Task<IActionResult> GetArticleById(Guid id)
        {
            try
            {
                var article = await _unitOfWork.Repository<Article>().GetById(id);

                if (article == null)
                {
                    _logger.LogWarning($"Article with id {id} not found.");
                    return new NotFoundResult();
                }

                var articleDto = _mapper.Map<ArticleDto>(article);
                return new OkObjectResult(articleDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetArticleById");
                return new StatusCodeResult(500);
            }
        }

        public async Task<IActionResult> GetAllArticles(string? range = null)
        {
            var queryParams = ParameterParser.ParseRangeAndSort(range, "sort");
            var page = queryParams.Page;
            var pageSize = queryParams.PerPage;


            try
            {
                var articles = await _unitOfWork.Repository<Article>().GetAll().OrderByDescending(a => a.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

                // Add Content-Range header

                _httpContextAccessor.HttpContext.Response.Headers.Add("Content-Range", $"articles {page * pageSize}-{(page + 1) * pageSize - 1}/{articles.Count}");

                return new OkObjectResult(articles.ToList());

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAllArticles");
                return new StatusCodeResult(500);
            }
        }

        public async Task<IActionResult> UpdateArticle(Guid id, ArticleUpdateDto updateArticle)
        {
            try
            {
                var article = await _unitOfWork.Repository<Article>().GetById(id);
                if (article == null)
                {
                    _logger.LogWarning($"Article with id {id} not found.");
                    return new NotFoundResult();
                }


                article.Title = updateArticle.Title;
                article.Content = updateArticle.Content;
                article.AuthorId = updateArticle.AuthorId;
                article.UpdatedAt = DateTime.UtcNow;
                article.ImageUrl = updateArticle.ImageUrl;
                article.SourceId = updateArticle.SourceId;
                article.CategoryId = updateArticle.CategoryId;


                await _unitOfWork.CompleteAsync();

                var updatedArticleDto = _mapper.Map<ArticleUpdateDto>(article);

                // Update article in elastic search
                await _elasticClient.UpdateAsync<Article>(id, u => u.Doc(article));

                return new OkObjectResult(updatedArticleDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateArticle");
                return new StatusCodeResult(500);
            }
        }

        public async Task<IActionResult> GetArticlesByCategory(int categoryId, string? categoryName, string? range = null)
        {
            try
            {

                if (categoryId != 0 && categoryName != null)
                {
                    _logger.LogWarning("Both categoryId and categoryName are provided. Please provide only one.");
                    return new BadRequestObjectResult(new { Message = "Both categoryId and categoryName are provided. Please provide only one." });
                }

                if (categoryId == 0 && categoryName == null)
                {
                    _logger.LogWarning("Please provide either categoryId or categoryName.");
                    return new BadRequestObjectResult(new { Message = "Please provide either categoryId or categoryName." });
                }


                var queryParams = ParameterParser.ParseRangeAndSort(range, "sort");
                var page = queryParams.Page;
                var pageSize = queryParams.PerPage;
                IQueryable<Article> articlesQuery;

                if (categoryName == null)
                {
                    articlesQuery = _unitOfWork.Repository<Article>().GetAll().OrderByDescending(a => a.CreatedAt).Where(a => a.CategoryId == categoryId);
                }
                else
                {
                    articlesQuery = _unitOfWork.Repository<Article>().GetAll().OrderByDescending(a => a.CreatedAt).Where(a => a.Category.Name == categoryName);
                }

                var articles = await articlesQuery.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

                if (articles.Count == 0)
                {
                    _logger.LogWarning($"No articles found for category {categoryName}.");
                    return new NotFoundResult();
                }

                // Content-Range header
                _httpContextAccessor.HttpContext.Response.Headers.Add("Content-Range", $"articles {page * pageSize}-{(page + 1) * pageSize - 1}/{articles.Count}");

                return new OkObjectResult(articles.ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetArticlesByCategory");
                return new StatusCodeResult(500);
            }
        }


        public async Task<IActionResult> GetArticlesByTag(string? tagName, string? range = null)
        {
            try
            {

                if (tagName == null)
                {
                    _logger.LogWarning("Please provide a tag name.");
                    return new BadRequestObjectResult(new { Message = "Please provide a tag name." });
                }

                var queryParams = ParameterParser.ParseRangeAndSort(range, "sort");
                var page = queryParams.Page;
                var pageSize = queryParams.PerPage;

                var articles = await _unitOfWork.Repository<Article>().GetAll().OrderByDescending(a => a.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).Where(a => a.Tags.Contains(tagName)).ToListAsync();

                if (articles.Count == 0)
                {
                    _logger.LogWarning($"No articles found for tag {tagName}.");
                    return new NotFoundResult();
                }

                // Content-Range header
                _httpContextAccessor.HttpContext.Response.Headers.Add("Content-Range", $"articles {page * pageSize}-{(page + 1) * pageSize - 1}/{articles.Count}");

                return new OkObjectResult(articles.ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetArticlesByTag");
                return new StatusCodeResult(500);
            }
        }

        public async Task<IActionResult> GetArticlesBySource(Guid sourceId, string? sourceName, string? range = null)
        {
            try
            {

                if (sourceId == Guid.Empty && sourceName == null)
                {
                    _logger.LogWarning("Please provide either sourceId or sourceName.");
                    return new BadRequestObjectResult(new { Message = "Please provide either sourceId or sourceName." });
                }

                if (sourceId != Guid.Empty && sourceName != null)
                {
                    _logger.LogWarning("Both sourceId and sourceName are provided. Please provide only one.");
                    return new BadRequestObjectResult(new { Message = "Both sourceId and sourceName are provided. Please provide only one." });
                }
                var queryParams = ParameterParser.ParseRangeAndSort(range, "sort");
                var page = queryParams.Page;
                var pageSize = queryParams.PerPage;
                var articles = new List<Article>();

                if (sourceName == null)
                {
                    articles = await _unitOfWork.Repository<Article>().GetAll().OrderByDescending(a => a.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).Where(a => a.SourceId == sourceId).ToListAsync();
                }
                else
                {
                    var source = await _unitOfWork.Repository<Source>().GetByCondition(s => s.Name == sourceName).FirstOrDefaultAsync();
                    articles = await _unitOfWork.Repository<Article>().GetAll().OrderByDescending(a => a.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).Where(a => a.SourceId == source.Id).ToListAsync();
                }

                if (articles.Count == 0)
                {
                    _logger.LogWarning($"No articles found for source {sourceName}.");
                    return new NotFoundResult();
                }

                // Content-Range header
                _httpContextAccessor.HttpContext.Response.Headers.Add("Content-Range", $"articles {page * pageSize}-{(page + 1) * pageSize - 1}/{articles.Count}");

                return new OkObjectResult(articles.ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetArticlesBySource");
                return new StatusCodeResult(500);
            }
        }
 /*
        public async Task<IActionResult> GetRecommendetArticles()
        {
            try
            {
                // Get refresh token from cookies
                var refreshToken = _httpContextAccessor.HttpContext.Request.Cookies["refreshToken"];

                // Get user agent from request headers
                var userAgent = _httpContextAccessor.HttpContext.Request.Headers["User-Agent"].ToString();

                if (refreshToken == null)
                {
                    var articlesF = await _unitOfWork.Repository<Article>().GetAll().OrderByDescending(a => a.CreatedAt).Take(5).ToListAsync();

                    return new OkObjectResult(articlesF);

                }

                var user = await FindUserByRefreshToken(refreshToken, userAgent);

                if (user == null)
                {
                    var articlesF = await _unitOfWork.Repository<Article>().GetAll().OrderByDescending(a => a.CreatedAt).Take(5).ToListAsync();

                    return new OkObjectResult(articlesF);
                }

                var userHistory = await _unitOfWork.Repository<UserHistory>().GetByCondition(uh => uh.UserId == user.Id).ToListAsync();

                // Get most used tags 
                var tags = userHistory.SelectMany(uh => uh.Tags.Split(",")).GroupBy(t => t).OrderBy(t => t.Count()).Select(t => t.Key).Take(5).ToList();

                var articles = await _unitOfWork.Repository<Article>().GetAll().OrderByDescending(a => a.CreatedAt).Where(a => tags.Any(t => a.Tags.Contains(t))).Take(5).ToListAsync();

                // Content-Range header
                _httpContextAccessor.HttpContext.Response.Headers.Add("Content-Range", $"articles 0-4/{articles.Count}");

                return new OkObjectResult(articles.ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetRecommendetArticles");
                return new BadRequestObjectResult(new { Messages = ex.Message });
            }
        }*/

        public async Task<IActionResult> GetTrendingArticles()
        {
            try
            {
                var currentTime = DateTime.UtcNow;
                var last24Hours = currentTime.AddHours(-24);

                var articles = await _unitOfWork.Repository<ArticleStats>()
                    .GetAll()
                    .Where(a => a.ViewTime >= last24Hours)
                    .GroupBy(a => a.ArticleId)
                    .Select(g => new
                    {
                        ArticleId = g.Key,
                        ViewCount = g.Count()
                    })
                    .OrderByDescending(g => g.ViewCount)
                    .Take(5)
                    .ToListAsync();

                // Loop through articles and get the article details

                var articleDetails = new List<Article>();

                foreach (var article in articles)
                {
                    var art = await _unitOfWork.Repository<Article>().GetById(article.ArticleId);
                    articleDetails.Add(art);
                }

                // Content-Range header
                _httpContextAccessor.HttpContext.Response.Headers.Add("Content-Range", $"articles 0-4/{articles.Count}");

                return new OkObjectResult(articleDetails.ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetTrendingArticles");
                return new StatusCodeResult(500);
            }
        }

        public async Task<IActionResult> GetRecommendedArticles()
        {
            try
            {
                // Get refresh token from cookies
                var refreshToken = _httpContextAccessor.HttpContext.Request.Cookies["refreshToken"];

                // Get user agent from request headers
                var userAgent = _httpContextAccessor.HttpContext.Request.Headers["User-Agent"].ToString();
                var ipAddress = _httpContextAccessor.HttpContext.Connection.RemoteIpAddress?.ToString();

                User user = null;
                if (!string.IsNullOrEmpty(refreshToken))
                {
                    user = await FindUserByRefreshToken(refreshToken, userAgent);
                }

                List<Article> recommendedArticles;

                if (user != null)
                {
                    // Get recommended articles for logged-in user
                    recommendedArticles = await GetRecommendedArticlesForUser(user.Id);
                }
                else
                {
                    // Get recommended articles for non-logged-in user
                    recommendedArticles = await GetRecommendedArticlesForNonLoggedInUser(ipAddress, userAgent);
                }

                // Content-Range header for pagination support
                _httpContextAccessor.HttpContext.Response.Headers.Add("Content-Range", $"articles 0-4/{recommendedArticles.Count}");

                return new OkObjectResult(recommendedArticles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetRecommendedArticles");
                return new BadRequestObjectResult(new { Message = "An error occurred while fetching recommended articles." });
            }
        }

        private async Task<List<Article>> GetRecommendedArticlesForUser(Guid userId)
        {
            var userHistory = await _unitOfWork.Repository<UserHistory>()
                .GetByCondition(uh => uh.UserId == userId)
                .ToListAsync();

            if (!userHistory.Any())
            {
                // If user has no history, return the latest articles
                return await _unitOfWork.Repository<Article>()
                    .GetAll()
                    .OrderByDescending(a => a.CreatedAt)
                    .Take(5)
                    .ToListAsync();
            }

            // Get most used tags from user history
            var tags = userHistory
                .Where(uh => !string.IsNullOrEmpty(uh.Tags))
                .SelectMany(uh => uh.Tags.Split(","))
                .GroupBy(t => t)
                .OrderByDescending(t => t.Count())
                .Select(t => t.Key)
                .Take(5)
                .ToList();

            // Get articles matching most used tags
            var recommendedArticles = await _unitOfWork.Repository<Article>()
                .GetAll()
                .Where(a => tags.Any(t => a.Tags.Contains(t)))
                .OrderByDescending(a => a.CreatedAt)
                .Take(5)
                .ToListAsync();

            return recommendedArticles.Any() ? recommendedArticles : await GetLatestArticles();
        }

        private async Task<List<Article>> GetRecommendedArticlesForNonLoggedInUser(string ipAddress, string userAgent)
        {

            var articles = await _unitOfWork.Repository<Article>().GetAll().OrderByDescending(a => a.CreatedAt).Take(5).ToListAsync();

            if(articles.Any())
            {
                return articles;
            }
         

            return await GetRecommendedArticlesForNearbyUsers(ipAddress) ?? await GetLatestArticles();
        }

        private async Task<List<Article>> GetRecommendedArticlesForNearbyUsers(string ipAddress)
        {
            var ipPrefix = string.Join(".", ipAddress.Split('.').Take(3)) + ".";

            var nearbyUserStats = await _unitOfWork.Repository<ArticleStats>()
                .GetByCondition(uh => uh.IpAddress.StartsWith(ipPrefix))
                .ToListAsync();

            if (nearbyUserStats.Any())
            {
                // Get most used tags from nearby users
                var tags = nearbyUserStats
                    .Where(uh => !string.IsNullOrEmpty(uh.Article.Tags))
                    .SelectMany(uh => uh.Article.Tags.Split(","))
                    .GroupBy(t => t)
                    .OrderByDescending(t => t.Count())
                    .Select(t => t.Key)
                    .Take(5)
                    .ToList();

                // Get articles matching most used tags
                var recommendedArticles = await _unitOfWork.Repository<Article>()
                    .GetAll()
                    .Where(a => tags.Any(t => a.Tags.Contains(t)))
                    .OrderByDescending(a => a.CreatedAt)
                    .Take(5)
                    .ToListAsync();

                return recommendedArticles;
            }

            return null;
        }

        public async Task<IActionResult> GetForYouArticles()
        {
            try
            {
                var refreshToken = _httpContextAccessor.HttpContext.Request.Cookies["refreshToken"];
                var userAgent = _httpContextAccessor.HttpContext.Request.Headers["User-Agent"].ToString();
                var ipAddress = _httpContextAccessor.HttpContext.Connection.RemoteIpAddress?.ToString();

                User user = null;
                if (!string.IsNullOrEmpty(refreshToken))
                {
                    user = await FindUserByRefreshToken(refreshToken, userAgent);
                }

                if (user == null)
                {
                    return new UnauthorizedResult();
                }

                // Get user preferences
                var userPreferences = await _unitOfWork.Repository<UserPreference>()
                    .GetByCondition(up => up.UserId == user.Id)
                    .Select(up => up.CategoryId)
                    .ToListAsync();

                if (!userPreferences.Any())
                {
                    var latestArticles = await GetLatestArticles();
                    return new OkObjectResult(latestArticles);
                }

                // Get bookmarked articles
                var bookmarkedArticles = await _unitOfWork.Repository<Bookmark>()
                    .GetByCondition(b => b.UserId == user.Id)
                    .Select(b => b.Article)
                    .ToListAsync();

                // Get recommended articles based on preferences and bookmarks
                var recommendedArticles = await _unitOfWork.Repository<Article>()
                    .GetByCondition(a => userPreferences.Contains(a.CategoryId))
                    .OrderByDescending(a => a.CreatedAt)
                    .Take(10)
                    .ToListAsync();

                var uniqueArticles = recommendedArticles
                    .Union(bookmarkedArticles)
                    .Distinct()
                    .OrderByDescending(a => a.CreatedAt)
                    .Take(10)
                    .ToList();

                return new OkObjectResult(uniqueArticles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetForYouArticles");
                return new BadRequestObjectResult(new { Message = "An error occurred while fetching recommended articles." });
            }
        }


        private async Task<List<Article>> GetLatestArticles()
        {
            return await _unitOfWork.Repository<Article>()
                .GetAll()
                .OrderByDescending(a => a.CreatedAt)
                .Take(5)
                .ToListAsync();
        }




        public async Task<User?> FindUserByRefreshToken(string refreshToken, string userAgent)
        {
            var currentTime = DateTime.UtcNow;

            var refreshTokenEntry = _dBContext.refreshTokens.FirstOrDefault(r => r.Token == refreshToken && r.Expires > currentTime && r.UserAgent == userAgent);

            if (refreshTokenEntry == null)
            {
                return null;
            }

            var userId = refreshTokenEntry.UserId;
            var refreshTokenVersion = refreshTokenEntry.TokenVersion;

            var user = await _dBContext.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return null;
            }

            if (user.TokenVersion != refreshTokenVersion)
            {
                return null;
            }

            return user;
        }

        public async Task<IActionResult> LikeArticle(Guid articleId)
        {
            try
            {
                var article = await _unitOfWork.Repository<Article>().GetById(articleId);

                if (article == null)
                {
                    _logger.LogWarning($"Article with id {articleId} not found.");
                    return new NotFoundResult();
                }

                article.Likes += 1;

                await _unitOfWork.CompleteAsync();

                return new OkResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in LikeArticle");
                return new StatusCodeResult(500);
            }
        }

        public async Task<IActionResult> AddView(Guid articleId)
        {
            try
            {

                // Get user agent from request headers
                var userAgent = _httpContextAccessor.HttpContext.Request.Headers["User-Agent"].ToString();
                var refreshToken = _httpContextAccessor.HttpContext.Request.Cookies["refreshToken"];
                var ipAddress = IP_Address.GetUserIp();

                var user = await FindUserByRefreshToken(refreshToken, userAgent);

                if (user != null)
                {
                    var articleStats = new ArticleStats
                    {
                        ArticleId = articleId,
                        UserId = user.Id,
                        ViewTime = DateTime.UtcNow,
                        UserAgent = userAgent,
                        IpAddress = ipAddress
                    };

                    _unitOfWork.Repository<ArticleStats>().Create(articleStats);
                    await _unitOfWork.CompleteAsync();
                }
                else
                {
                    var articleStats = new ArticleStats
                    {
                        ArticleId = articleId,
                        ViewTime = DateTime.UtcNow,
                        UserAgent = userAgent,
                        IpAddress = ipAddress
                    };

                    _unitOfWork.Repository<ArticleStats>().Create(articleStats);
                    await _unitOfWork.CompleteAsync();

                }


                var article = await _unitOfWork.Repository<Article>().GetById(articleId);

                if (article == null)
                {
                    _logger.LogWarning($"Article with id {articleId} not found.");
                    return new NotFoundResult();
                }

                article.Views += 1;

                await _unitOfWork.CompleteAsync();

                // Add to user history

                if (user != null)
                {
                    var userHistory = new UserHistory
                    {
                        UserId = user.Id,
                        ArticleId = articleId,
                        Tags = article.Tags,
                        Date = DateTime.UtcNow,
                        IpAddress = ipAddress,
                        UserAgent = userAgent
                    };

                    _unitOfWork.Repository<UserHistory>().Create(userHistory);
                    await _unitOfWork.CompleteAsync();
                }

                return new OkResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AddView");
                return new StatusCodeResult(500);
            }
        }

        public async Task<IEnumerable<Article>> SearchArticlesAsync(string query, string? range = null)
        {

            var queryParams = ParameterParser.ParseRangeAndSort(range, "sort");
            var page = queryParams.Page;
            var pageSize = queryParams.PerPage;

            if (page == 0)
            {

            } else
            {
                page -= 1;
            }

            var response = await _elasticClient.SearchAsync<Article>(s => s
                    .From(page)
                    .Size(pageSize)
                    .Query(q => q
                        .MultiMatch(m => m
                            .Fields(f => f
                                .Field(p => p.Title, 2.0)
                                .Field(p => p.Content))
                            .Query(query)))
                    .Highlight(h => h
                        .Fields(
                            h => h.Field(p => p.Title),
                            h => h.Field(p => p.Content))
                        .PreTags("<em>")
                        .PostTags("</em>")));

            if (response.IsValid)
            {
                foreach (var hit in response.Hits)
                {
                    var article = hit.Source;
                    if (hit.Highlight.TryGetValue("title", out var titleHighlights))
                    {
                        article.Title = string.Join(" ", titleHighlights);
                    }
                    if (hit.Highlight.TryGetValue("content", out var contentHighlights))
                    {
                        article.Content = string.Join(" ", contentHighlights);
                    }
                }

                return response.Documents.ToList<Article>();
            }

            return Enumerable.Empty<Article>();
        }

    }

}
