using Microsoft.AspNetCore.Mvc;
using NewsAggregation.DTO.Subscriptions;
using ServiceStack;

namespace NewsAggregation.Services.Interfaces
{
    public interface ISubscriptionsService : IService
    {
        public Task<IActionResult> GetSubscriptionById(Guid id);
        public Task<IActionResult> GetAllSubscriptions(string? range = null);
        public Task<IActionResult> GetAllActiveSubscriptions(string? range = null);
        public Task<IActionResult> CreateSubscription(SubscriptionCreateDto subscriptionRequest);
        public Task<IActionResult> UpdateSubscription(Guid id, SubscriptionCreateDto subscriptionRequest);
        public Task<IActionResult> DeleteSubscription(Guid id);
    }

}
