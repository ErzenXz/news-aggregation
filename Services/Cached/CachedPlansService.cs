using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using NewsAggregation.DTO.Plans;
using NewsAggregation.Services.Interfaces;

namespace NewsAggregation.Services.Cached
{
    public class CachedPlansService : IPlansService
    {
        private readonly IPlansService _decorated;
        private readonly IDistributedCache _redis;

        public CachedPlansService(IPlansService plansService, IDistributedCache redis)
        {
            _decorated = plansService;
            _redis = redis;
        }

        public async Task<IActionResult> GetAllPlans(string? range = null)
        {
            string cacheKey = $"plans-{range}";

            var cachedResult = await _redis.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedResult))
            {
                return JsonConvert.DeserializeObject<dynamic>(cachedResult);

            }

            var result = await _decorated.GetAllPlans(range);

            var serializedResult = JsonConvert.SerializeObject(result);

            await _redis.SetStringAsync(cacheKey, serializedResult, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            });

            return result;
        }

        public async Task<IActionResult> GetAllActivePlans(string? range = null)
        {
            string cacheKey = $"active-plans-{range}";

            var cachedResult = await _redis.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedResult))
            {
                return JsonConvert.DeserializeObject<dynamic>(cachedResult);

            }

            var result = await _decorated.GetAllActivePlans(range);


            var serializedResult = JsonConvert.SerializeObject(result);

            await _redis.SetStringAsync(cacheKey, serializedResult, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            });


            return result;
        }

        public async Task<IActionResult> GetPlanById(Guid id)
        {
            string cacheKey = $"plan-{id}";
            var cachedResult = await _redis.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedResult))
            {
                return JsonConvert.DeserializeObject<dynamic>(cachedResult);
            }

            var result = await _decorated.GetPlanById(id);


            var serializedResult = JsonConvert.SerializeObject(result);

            await _redis.SetStringAsync(cacheKey, serializedResult, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            });


            return result;
        }

        public async Task<IActionResult> CreatePlan(PlanCreateDto planRequest)
        {
            var result = await _decorated.CreatePlan(planRequest);
            return result;
        }

        public async Task<IActionResult> UpdatePlan(Guid id, PlanCreateDto planRequest)
        {
            var result = await _decorated.UpdatePlan(id, planRequest);

            if (result is OkObjectResult)
            {
                await _redis.RemoveAsync($"plan-{id}");
            }

            return result;
        }

        public async Task<IActionResult> DeletePlan(Guid id)
        {
            var result = await _decorated.DeletePlan(id);

            if (result is OkObjectResult)
            {
                await _redis.RemoveAsync($"plan-{id}");
            }

            return result;
        }
    }
}
