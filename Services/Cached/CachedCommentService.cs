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

        public async Task<PagedInfo<CommentDto>> CommentsListView(string searchByUser, int page, int pageSize, Guid articleId)
        {
            string cacheKey = $"comments-{searchByUser}-{page}-{pageSize}-{articleId}";
            
            var cachedResult = _cache.GetString(cacheKey);
            if (!string.IsNullOrEmpty(cachedResult))
            {
                return JsonConvert.DeserializeObject<PagedInfo<CommentDto>>(cachedResult);
            }

           var result = await _decorated.CommentsListView(searchByUser, page, pageSize, articleId);

            var serializedResult = JsonConvert.SerializeObject(result);

            _cache.SetString(cacheKey, serializedResult, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(3)
            });

            return result;
        }

        public Task CreateComment(CommentCreateDto comment)
        {
            return _decorated.CreateComment(comment);
        }

        public Task DeleteComment(Guid id)
        {
            return _decorated.DeleteComment(id);
        }

        public async Task<List<Comment>> GetAllComments(string? range = null)
        {
            string cacheKey = $"comments-{range}";
            var cachedResult = _cache.GetString(cacheKey);

            if (!string.IsNullOrEmpty(cachedResult))
            {
                return JsonConvert.DeserializeObject<List<Comment>>(cachedResult);
            }

            var result = await _decorated.GetAllComments(range);


            var serializedResult = JsonConvert.SerializeObject(result);

            _cache.SetString(cacheKey, serializedResult, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(3)
            });

            return result;
        }


        public async Task<Comment> GetCommentById(Guid id)
        {
            string cacheKey = $"comment-{id}";
            var cachedResult = _cache.GetString(cacheKey);
            if (!string.IsNullOrEmpty(cachedResult))
            {
                return JsonConvert.DeserializeObject<Comment>(cachedResult);
            }

            var result = await _decorated.GetCommentById(id);

            var serializedResult = JsonConvert.SerializeObject(result);

            _cache.SetString(cacheKey, serializedResult, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(3)
            });
            return result;
        }

        public async Task<List<Comment>> GetCommentsByArticleId(Guid articleId)
        {
            string cacheKey = $"comments-{articleId}";
            var cachedResult = _cache.GetString(cacheKey);

            if (!string.IsNullOrEmpty(cachedResult))
            {
                return JsonConvert.DeserializeObject<List<Comment>>(cachedResult);
            }

            var result = await _decorated.GetCommentsByArticleId(articleId);

            var serializedResult = JsonConvert.SerializeObject(result);

            _cache.SetString(cacheKey, serializedResult, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(3)
            });
            return result;
        }

        public Task UpdateComment(Guid id, CommentDto comment)
        {
            return _decorated.UpdateComment(id, comment);
        }
    }
}
