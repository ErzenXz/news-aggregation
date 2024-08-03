using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using NewsAggregation.DTO.Category;
using NewsAggregation.Services.Interfaces;
using Newtonsoft.Json;

namespace NewsAggregation.Services.Cached;

public class CachedCategoryService : ICategoryService
{
    private readonly CategoryService _decorated;
    private readonly IDistributedCache _redisCache;

    public CachedCategoryService(CategoryService decorated, IDistributedCache redisCache)
    {
        _decorated = decorated;
        _redisCache = redisCache;
    }
    public async Task<IActionResult> CreateCategory(CategoryCreateDto createCategory)
    {
        return new OkObjectResult(await _decorated.CreateCategory(createCategory));
    }
    
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var result = await _decorated.DeleteCategory(id);

        if (result is OkResult)
        {
            await _redisCache.RemoveAsync($"category-{id}");
        }

        return new OkObjectResult(result);
    }

    public async Task<IActionResult> GetCategoryByName(string name)
    {
        string cacheKey = $"category-{name}";
        var cachedResult = _redisCache.GetString(cacheKey);

        if (!string.IsNullOrEmpty(cachedResult))
        {
            var cachedData = JsonConvert.DeserializeObject<dynamic>(cachedResult);
            return new OkObjectResult(cachedData);
        }

        var result = await _decorated.GetCategoryByName(name);

        var serializedResult = JsonConvert.SerializeObject(result);
        await _redisCache.SetStringAsync(cacheKey, serializedResult, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
        });

        return new OkObjectResult(result);
    }

    public async Task<IActionResult> GetAllCategories(string? range=null)
    {
        string cacheKey = $"categories-{range}";
        var cachedResult = await _redisCache.GetStringAsync(cacheKey);

        if (!string.IsNullOrEmpty(cachedResult))
        {
            var cachedData = JsonConvert.DeserializeObject<dynamic>(cachedResult);
            return new OkObjectResult(cachedData);
        }

        var result = await _decorated.GetAllCategories(range);

        var serializedResult = JsonConvert.SerializeObject(result);
        await _redisCache.SetStringAsync(cacheKey, serializedResult, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(3)
        });

        return new OkObjectResult(result);
    }

    public async Task<IActionResult> UpdateCategory(int id, CategoryCreateDto updateCategory)
    {
        var result = await _decorated.UpdateCategory(id, updateCategory);

        if (result is OkResult)
        {
            _redisCache.RemoveAsync($"category-{id}");
        }

        return new OkObjectResult(result);
    }

    public async Task<IActionResult> GetCategoryById(int id)
    {
        string cacheKey = $"category-{id}";
        var cachedResult = _redisCache.GetString(cacheKey);

        if (!string.IsNullOrEmpty(cachedResult))
        {
            var cachedData = JsonConvert.DeserializeObject<dynamic>(cachedResult);
            return new OkObjectResult(cachedData);
        }

        var result = await _decorated.GetCategoryById(id);

        var serializedResult = JsonConvert.SerializeObject(result);
        await _redisCache.SetStringAsync(cacheKey, serializedResult, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        });

        return new OkObjectResult(result);
    }
}