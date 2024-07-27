using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using NewsAggregation.DTO.Article;
using NewsAggregation.Helpers;
using NewsAggregation.Services.Interfaces;
using Newtonsoft.Json;
using System;

namespace NewsAggregation.Services.Cached
{
    public class CachedArticleService : IArticleService
    {

        private readonly ArticleService _decorated;
        private readonly IDistributedCache _redisCache;

        public CachedArticleService(ArticleService articleService, IDistributedCache redisCache)
        {
            _decorated = articleService;
            _redisCache = redisCache;
        }

        

        public Task<IActionResult> CreateArticle(ArticleCreateDto article)
        {
            return _decorated.CreateArticle(article);
        }

        public Task<IActionResult> DeleteArticle(Guid id)
        {
            return _decorated.DeleteArticle(id);
        }

        public async Task<IActionResult> GetAllArticles(string? range = null)
        {
            string cacheKey = $"range-{range}";
            var cachedResult = await _redisCache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedResult))
            {
                var cachedData = JsonConvert.DeserializeObject<dynamic>(cachedResult);
                return new OkObjectResult(cachedData);
            }

            var result = await _decorated.GetAllArticles(range);

            var serializedResult = JsonConvert.SerializeObject(result);
            await _redisCache.SetStringAsync(cacheKey, serializedResult, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1)
            });

            return new OkObjectResult(result);
        }


        public async Task<IActionResult> GetArticleById(Guid id)
        {
            string cacheKey = $"article-{id}";
            var cachedResult = _redisCache.GetString(cacheKey);

            if (!string.IsNullOrEmpty(cachedResult))
            {
                var cachedData = JsonConvert.DeserializeObject<dynamic>(cachedResult);
                return new OkObjectResult(cachedData);
            }

            var result = await _decorated.GetArticleById(id);

            var serializedResult = JsonConvert.SerializeObject(result);
            await _redisCache.SetStringAsync(cacheKey, serializedResult, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(20)
            });

            return new OkObjectResult(result);
        }
        

        public Task<IActionResult> GetRecommendetArticles()
        {
            return _decorated.GetRecommendetArticles();
        }

        public async Task<IActionResult> GetTrendingArticles()
        {
            string cacheKey = $"trending-articles";
            var cachedResult = await _redisCache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedResult))
            {
                var cachedData = JsonConvert.DeserializeObject<dynamic>(cachedResult);
                return new OkObjectResult(cachedData);
            }

            var result = await _decorated.GetTrendingArticles();

            var serializedResult = JsonConvert.SerializeObject(result);
            await _redisCache.SetStringAsync(cacheKey, serializedResult, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(3)
            });

            return new OkObjectResult(result);
        }

        public Task<PagedInfo<ArticleDto>> PagedArticlesView(int page, int pageSize, string searchByTitle)
        {
            return _decorated.PagedArticlesView(page, pageSize, searchByTitle);
        }

        public Task<IActionResult> UpdateArticle(Guid id, ArticleUpdateDto updateArticle)
        {
            return _decorated.UpdateArticle(id, updateArticle);
        }

        public Task<IActionResult> GetArticlesByCategory(int categoryId, string? categoryName, string? range = null)
        {
            return _decorated.GetArticlesByCategory(categoryId, categoryName, range);
        }

        public Task<IActionResult> GetArticlesByTag(string? tagName, string? range = null)
        {
            return _decorated.GetArticlesByTag(tagName, range);
        }

        public Task<IActionResult> GetArticlesBySource(Guid sourceId, string? sourceName, string? range = null)
        {
            return _decorated.GetArticlesBySource(sourceId, sourceName, range);
        }

        public Task<IActionResult> LikeArticle(Guid articleId)
        {
            return _decorated.LikeArticle(articleId);
        }

        public Task<IActionResult> AddView(Guid articleId)
        {
            return _decorated.AddView(articleId);
        }

    }
}
