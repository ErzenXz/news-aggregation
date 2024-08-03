using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using News_aggregation.Entities;
using NewsAggregation.DTO.Comment;
using NewsAggregation.Helpers;
using NewsAggregation.Services.Interfaces;
using Newtonsoft.Json;

namespace NewsAggregation.Services.Cached
{
    public class CachedCommentService : ICommentService
    {
        private readonly CommentService _decorated;
        private readonly IDistributedCache _cache;

        public CachedCommentService(CommentService articleService, IDistributedCache cache)
        {
            _decorated = articleService;
            _cache = cache;
        }

        public Task<IActionResult> CreateComment(CommentCreateDto comment)
        {
            return _decorated.CreateComment(comment);
        }

        public Task<IActionResult> DeleteComment(Guid id)
        {
            return _decorated.DeleteComment(id);
        }

        public async Task<IActionResult> GetAllComments(string? range = null)
        {
            string cacheKey = $"comments-{range}";
            var cachedResult = _cache.GetString(cacheKey);

            if (!string.IsNullOrEmpty(cachedResult))
            {
                return new OkObjectResult(JsonConvert.DeserializeObject<dynamic>(cachedResult));
            }

            var result = await _decorated.GetAllComments(range);


            var serializedResult = JsonConvert.SerializeObject(result);

            _cache.SetString(cacheKey, serializedResult, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(3)
            });

            return new OkObjectResult(result);
        }


        public async Task<IActionResult> GetCommentById(Guid id)
        {
            string cacheKey = $"comment-{id}";
            var cachedResult = _cache.GetString(cacheKey);
            if (!string.IsNullOrEmpty(cachedResult))
            {
                return new OkObjectResult(JsonConvert.DeserializeObject<dynamic>(cachedResult));
            }

            var result = await _decorated.GetCommentById(id);

            var serializedResult = JsonConvert.SerializeObject(result);

            _cache.SetString(cacheKey, serializedResult, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(3)
            });
            return new OkObjectResult(result);
        }

        public async Task<IActionResult> GetCommentsByArticleId(Guid articleId)
        {
            string cacheKey = $"comments-{articleId}";
            var cachedResult = _cache.GetString(cacheKey);

            if (!string.IsNullOrEmpty(cachedResult))
            {
                return new OkObjectResult(JsonConvert.DeserializeObject<dynamic>(cachedResult));
            }

            var result = await _decorated.GetCommentsByArticleId(articleId);

            var serializedResult = JsonConvert.SerializeObject(result);

            _cache.SetString(cacheKey, serializedResult, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(3)
            });
            return new OkObjectResult(result);
        }

        public Task<IActionResult> UpdateComment(Guid id, CommentDto comment)
        {
            return _decorated.UpdateComment(id, comment);
        }

        public Task<IActionResult> ReportComment(CommentReportDto commentReport)
        {
            return _decorated.ReportComment(commentReport);
        }
    }
}
