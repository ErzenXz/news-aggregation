using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using NewsAggregation.DTO.Ads;
using NewsAggregation.Services.Interfaces;
using Newtonsoft.Json;

namespace NewsAggregation.Services.Cached;

public class CachedAdsService : IAdsService
{
    private readonly AdsService _decorated;
    private readonly IDistributedCache _redisCache;

    public CachedAdsService(AdsService decorated, IDistributedCache redisCache)
    {
        _decorated = decorated;
        _redisCache = redisCache;
    }
    public async Task<IActionResult> GetAd(Guid id)
    {
        string cacheKey = $"ad-{id}";
        var cachedResult = _redisCache.GetString(cacheKey);

        if (!string.IsNullOrEmpty(cachedResult))
        {
            var cachedData = JsonConvert.DeserializeObject<dynamic>(cachedResult);
            return new OkObjectResult(cachedData);
        }

        var result = await _decorated.GetAd(id);

        var serializedResult = JsonConvert.SerializeObject(result);
        await _redisCache.SetStringAsync(cacheKey, serializedResult, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
        });

        return new OkObjectResult(result);
    }

    public async Task<IActionResult> GetAllAds(string? range = null)
    {
        string cacheKey = $"range-{range}";
        var cachedResult = await _redisCache.GetStringAsync(cacheKey);

        if (!string.IsNullOrEmpty(cachedResult))
        {
            var cachedData = JsonConvert.DeserializeObject<dynamic>(cachedResult);
            return new OkObjectResult(cachedData);
        }

        var result = await _decorated.GetAllAds(range);

        var serializedResult = JsonConvert.SerializeObject(result);
        await _redisCache.SetStringAsync(cacheKey, serializedResult, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
        });

        return new OkObjectResult(result);
    }

    public async Task<IActionResult> GetAllActiveAds(string? range = null)
    {
        string cacheKey = $"trending-ads";
        var cachedResult = await _redisCache.GetStringAsync(cacheKey);

        if (!string.IsNullOrEmpty(cachedResult))
        {
            var cachedData = JsonConvert.DeserializeObject<dynamic>(cachedResult);
            return new OkObjectResult(cachedData);
        }

        var result = await _decorated.GetAllActiveAds();

        var serializedResult = JsonConvert.SerializeObject(result);
        await _redisCache.SetStringAsync(cacheKey, serializedResult, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(3)
        });

        return new OkObjectResult(result);
    }

    public Task<IActionResult> CreateAd(AdCreateDto adRequest)
    {
        return _decorated.CreateAd(adRequest);
    }

    public Task<IActionResult> UpdateAd(Guid id, AdCreateDto adRequest)
    {
        return _decorated.UpdateAd(id, adRequest);
    }

    public Task<IActionResult> DeleteAd(Guid id)
    {
        return _decorated.DeleteAd(id);
    }
}