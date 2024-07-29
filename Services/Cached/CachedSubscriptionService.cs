﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using NewsAggregation.DTO.Subscriptions;
using NewsAggregation.Services.Interfaces;

namespace NewsAggregation.Services.Cached
{
    public class CachedSubscriptionService : ISubscriptionsService
    {
        private readonly ISubscriptionsService _decorated;
        private readonly IDistributedCache _redis;

        public CachedSubscriptionService(ISubscriptionsService subscriptionsService, IDistributedCache redis)
        {
            _decorated = subscriptionsService;
            _redis = redis;
        }

        public async Task<IActionResult> GetAllSubscriptions(string? range = null)
        {
            string cacheKey = $"subscriptions-{range}";

            var cachedResult = await _redis.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedResult))
            {
                return JsonConvert.DeserializeObject<dynamic>(cachedResult);

            }

            var result = await _decorated.GetAllSubscriptions(range);


            var serializedResult = JsonConvert.SerializeObject(result);

            await _redis.SetStringAsync(cacheKey, serializedResult, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            });


            return result;
        }

        public async Task<IActionResult> GetAllActiveSubscriptions(string? range = null)
        {
            string cacheKey = $"active-subscriptions-{range}";

            var cachedResult = await _redis.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedResult))
            {
                return JsonConvert.DeserializeObject<dynamic>(cachedResult);
            }

            var result = await _decorated.GetAllActiveSubscriptions(range);


            var serializedResult = JsonConvert.SerializeObject(result);

            await _redis.SetStringAsync(cacheKey, serializedResult, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            });


            return result;
        }

        public async Task<IActionResult> GetSubscriptionById(Guid id)
        {
            string cacheKey = $"subscription-{id}";
            var cachedResult = await _redis.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedResult))
            {
                return JsonConvert.DeserializeObject<dynamic>(cachedResult);
            }

            var result = await _decorated.GetSubscriptionById(id);


            var serializedResult = JsonConvert.SerializeObject(result);

            await _redis.SetStringAsync(cacheKey, serializedResult, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            });


            return result;
        }

        public async Task<IActionResult> CreateSubscription(SubscriptionCreateDto subscriptionRequest)
        {
            var result = await _decorated.CreateSubscription(subscriptionRequest);
            return result;
        }

        public async Task<IActionResult> UpdateSubscription(Guid id, SubscriptionCreateDto subscriptionRequest)
        {
            var result = await _decorated.UpdateSubscription(id, subscriptionRequest);

            if (result is OkResult)
            {

                await _redis.RemoveAsync($"subscription-{id}");

            }

            return result;
        }

        public async Task<IActionResult> DeleteSubscription(Guid id)
        {
            var result = await _decorated.DeleteSubscription(id);

            if (result is OkResult)
            {

                await _redis.RemoveAsync($"subscription-{id}");
            }

            return result;
        }
    }
}