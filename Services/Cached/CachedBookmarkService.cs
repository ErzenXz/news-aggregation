using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using NewsAggregation.DTO.Favorite;
using NewsAggregation.Services.Interfaces;
using Newtonsoft.Json;

namespace NewsAggregation.Services.Cached;

public class CachedBookmarkService : IBookmarkService
{
    private readonly BookmarkService _decorated;
    private readonly IDistributedCache _redisCache;


    public CachedBookmarkService(BookmarkService decorated, IDistributedCache redisCache)
    {
        _decorated = decorated;
        _redisCache = redisCache;
    }

    public async Task<IActionResult> GetAllBookmarks(string? range = null)
    {
        string cacheKey = $"range-{range}";
        var cachedResult = await _redisCache.GetStringAsync(cacheKey);

        if (!string.IsNullOrEmpty(cachedResult))
        {
            var cachedData = JsonConvert.DeserializeObject<dynamic>(cachedResult);
            return new OkObjectResult(cachedData);
        }

        var result = await _decorated.GetAllBookmarks(range);

        var serializedResult = JsonConvert.SerializeObject(result);
        await _redisCache.SetStringAsync(cacheKey, serializedResult, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
        });

        return new OkObjectResult(result);
    }

    public async Task<IActionResult> GetBookmarksByArticleId(Guid articleId)
    {
        string cacheKey = $"bookmarks-{articleId}";
        var cachedResult = _redisCache.GetString(cacheKey);

        if (!string.IsNullOrEmpty(cachedResult))
        {
            return JsonConvert.DeserializeObject<dynamic>(cachedResult);
        }

        var result = await _decorated.GetBookmarksByArticleId(articleId);

        var serializedResult = JsonConvert.SerializeObject(result);

        _redisCache.SetString(cacheKey, serializedResult, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
        });
        return result;
    }
 
    public async Task<IActionResult> GetBookmarkById(Guid id)
    {
        string cacheKey = $"$bookmark-{id}";
        var cachedResult = _redisCache.GetString(cacheKey);
        
        if (!string.IsNullOrEmpty(cachedResult))
        {
            var cachedData = JsonConvert.DeserializeObject<dynamic>(cachedResult);
            return new OkObjectResult(cachedData);
        }

        var result = await _decorated.GetBookmarkById(id);
        
        var serializedResult = JsonConvert.SerializeObject(result);
        await _redisCache.SetStringAsync(cacheKey, serializedResult, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
        });

        return new OkObjectResult(result);
    }

    public async Task<IActionResult> CreateBookmark(BookmarkCreateDto bookmark)
    {
        return await _decorated.CreateBookmark(bookmark);
    }

    public async Task<IActionResult> DeleteBookmark(Guid id)
    {
        var result = await _decorated.DeleteBookmark(id);
        
        if (result is OkResult)
        {
            await _redisCache.RemoveAsync($"bookmark-{id}");
        }

        return result;
    }
}