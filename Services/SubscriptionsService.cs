using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewsAggregation.Data;
using NewsAggregation.DTO.Subscriptions;
using NewsAggregation.Models;
using NewsAggregation.Services.Interfaces;

namespace NewsAggregation.Services
{
    public class SubscriptionsService : ISubscriptionsService
    {
        private readonly DBContext _dBContext;
        private readonly ILogger<AuthService> _logger;

        public SubscriptionsService(DBContext dBContext, ILogger<AuthService> logger)
        {
            _dBContext = dBContext;
            _logger = logger;
        }

        public async Task<IActionResult> GetAllSubscriptions(string? range = null)
        {
            var queryParams = ParameterParser.ParseRangeAndSort(range, "sort");

            try
            {
                var now = DateTime.UtcNow;

                var subscriptions = await _dBContext.Subscriptions
                    .Skip((queryParams.Page - 1) * queryParams.PerPage)
                    .Take(queryParams.PerPage)
                    .ToListAsync();

                return new OkObjectResult(subscriptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetSubscriptions");
                return new StatusCodeResult(500);
            }
        }

        public async Task<IActionResult> GetAllActiveSubscriptions(string? range = null)
        {
            var queryParams = ParameterParser.ParseRangeAndSort(range, "sort");

            try
            {
                var now = DateTime.UtcNow;

                var subscriptions = await _dBContext.Subscriptions
                    .Where(s => s.StartDate <= now && s.EndDate >= now)
                    .Skip((queryParams.Page - 1) * queryParams.PerPage)
                    .Take(queryParams.PerPage)
                    .ToListAsync();

                return new OkObjectResult(subscriptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetSubscriptions");
                return new StatusCodeResult(500);
            }
        }

        public async Task<IActionResult> GetSubscriptionById(Guid id)
        {
            try
            {
                var subscription = await _dBContext.Subscriptions
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (subscription == null)
                {
                    return new NotFoundResult();
                }

                return new OkObjectResult(subscription);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetSubscriptionById");
                return new StatusCodeResult(500);
            }
        }

        public async Task<IActionResult> CreateSubscription(SubscriptionCreateDto subscriptionRequest)
        {
            try
            {
                var subscription = new Subscriptions
                {
                    UserId = subscriptionRequest.UserId,
                    PlanId = subscriptionRequest.PlanId,
                    StartDate = subscriptionRequest.StartDate,
                    EndDate = subscriptionRequest.EndDate,
                    Amount = subscriptionRequest.Amount,
                    Currency = subscriptionRequest.Currency
                };

                await _dBContext.Subscriptions.AddAsync(subscription);
                await _dBContext.SaveChangesAsync();

                return new OkObjectResult(subscription);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreateSubscription");
                return new StatusCodeResult(500);
            }
        }

        public async Task<IActionResult> UpdateSubscription(Guid id, SubscriptionCreateDto subscriptionRequest)
        {
            try
            {
                var subscription = await _dBContext.Subscriptions
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (subscription == null)
                {
                    return new NotFoundResult();
                }

                subscription.UserId = subscriptionRequest.UserId;
                subscription.PlanId = subscriptionRequest.PlanId;
                subscription.StartDate = subscriptionRequest.StartDate;
                subscription.EndDate = subscriptionRequest.EndDate;
                subscription.Amount = subscriptionRequest.Amount;
                subscription.Currency = subscriptionRequest.Currency;

                _dBContext.Subscriptions.Update(subscription);
                await _dBContext.SaveChangesAsync();

                return new OkObjectResult(subscription);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateSubscription");
                return new StatusCodeResult(500);
            }
        }

        public async Task<IActionResult> DeleteSubscription(Guid id)
        {
            try
            {
                var subscription = await _dBContext.Subscriptions
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (subscription == null)
                {
                    return new NotFoundResult();
                }

                _dBContext.Subscriptions.Remove(subscription);
                await _dBContext.SaveChangesAsync();

                return new OkResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteSubscription");
                return new StatusCodeResult(500);
            }
        }
    }
}
