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
        string cacheKey = $"ads-{range}";
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

    public async Task<IActionResult> GetPersonalizedAds(string? range = null)
    {
        return await _decorated.GetPersonalizedAds(range);
    }

    public async Task<IActionResult> CreateAd(AdCreateDto adRequest)
    {
        return await _decorated.CreateAd(adRequest);
    }

    public async Task<IActionResult> UpdateAd(Guid id, AdCreateDto adRequest)
    {
        var result = await _decorated.UpdateAd(id, adRequest);
        
        if (result is OkResult)
        {
            await _redisCache.RemoveAsync($"ad-{id}");
        }
        return result;
    }

    public async Task<IActionResult> DeleteAd(Guid id)
    {
        var result = await _decorated.DeleteAd(id);
        
        if (result is OkResult)
        {
            await _redisCache.RemoveAsync($"ad-{id}");
        }

        return result;
    }
}