using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewsAggregation.Data;
using NewsAggregation.Data.UnitOfWork;
using NewsAggregation.DTO.Subscriptions;
using NewsAggregation.Models;
using NewsAggregation.Services.Interfaces;

namespace NewsAggregation.Services
{
    public class SubscriptionsService : ISubscriptionsService
    {
        private readonly ILogger<AuthService> _logger;
        private readonly IUnitOfWork _unitOfWork;

        public SubscriptionsService(ILogger<AuthService> logger, IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        public async Task<IActionResult> GetAllSubscriptions(string? range = null)
        {
            var queryParams = ParameterParser.ParseRangeAndSort(range, "sort");
            var page = queryParams.Page;
            var pageSize = queryParams.PerPage;

            try
            {
                var now = DateTime.UtcNow;

                var subscriptions = await _unitOfWork.Repository<Subscriptions>().GetAll()
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
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
            var page = queryParams.Page;
            var pageSize = queryParams.PerPage;

            try
            {
                var now = DateTime.UtcNow;

                var subscriptions = await _unitOfWork.Repository<Subscriptions>().GetAll()
                    .Where(s => s.StartDate <= now && s.EndDate >= now)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
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
                var subscription = await _unitOfWork.Repository<Subscriptions>().GetById(id);

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

                _unitOfWork.Repository<Subscriptions>().Create(subscription);
                await _unitOfWork.CompleteAsync();

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
                var subscription = await _unitOfWork.Repository<Subscriptions>().GetById(id);

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

                _unitOfWork.Repository<Subscriptions>().Update(subscription);
                await _unitOfWork.CompleteAsync();

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
                var subscription = await _unitOfWork.Repository<Subscriptions>().GetById(id);

                if (subscription == null)
                {
                    return new NotFoundResult();
                }

                _unitOfWork.Repository<Subscriptions>().Delete(subscription);
                await _unitOfWork.CompleteAsync();

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
