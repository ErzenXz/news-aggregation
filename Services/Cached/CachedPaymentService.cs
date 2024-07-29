using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using NewsAggregation.DTO.Payments;
using NewsAggregation.Services.Interfaces;

namespace NewsAggregation.Services.Cached
{
    public class CachedPaymentService : IPaymentService
    {
        private readonly IPaymentService _decorated;
        private readonly IDistributedCache _redis;

        public CachedPaymentService(IPaymentService paymentService, IDistributedCache redis)
        {
            _decorated = paymentService;
            _redis = redis;
        }

        public async Task<IActionResult> GetAllPayments(string? range = null)
        {
            string cacheKey = $"payments-{range}";

            var cachedResult = await _redis.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedResult))
            {
                return JsonConvert.DeserializeObject<dynamic>(cachedResult);
            }

            var result = await _decorated.GetAllPayments(range);

            var serializedResult = JsonConvert.SerializeObject(result);

            await _redis.SetStringAsync(cacheKey, serializedResult, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            });


            return result;
        }

        public async Task<IActionResult> GetPaymentById(Guid id)
        {
            string cacheKey = $"payment-{id}";
            var cachedResult = await _redis.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedResult))
            {
                return JsonConvert.DeserializeObject<dynamic>(cachedResult);

            }

            var result = await _decorated.GetPaymentById(id);

            var serializedResult = JsonConvert.SerializeObject(result);

            await _redis.SetStringAsync(cacheKey, serializedResult, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            });

            return result;

        }

        public async Task<IActionResult> UpdatePayment(Guid id, PaymentCreateDto paymentRequest)
        {
            var result = await _decorated.UpdatePayment(id, paymentRequest);

            if (result is OkResult)
            {
                await _redis.RemoveAsync($"payment-{id}");
            }

            return result;
        }

        public async Task<IActionResult> CreatePayment(PaymentCreateDto paymentRequest)
        {
            var result = await _decorated.CreatePayment(paymentRequest);
            return result;
        }

        public async Task<IActionResult> DeletePayment(Guid id)
        {
            var result = await _decorated.DeletePayment(id);

            if (result is OkResult)
            {
                await _redis.RemoveAsync($"payment-{id}");
            }

            return result;
        }
    }
}
