using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewsAggregation.Data;
using NewsAggregation.DTO.Subscriptions;
using NewsAggregation.Services.Interfaces;

namespace NewsAggregation.Controllers
{
    [Route("subscriptions")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ApiController]
    public class SubscriptionsController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly DBContext _dBContext;
        private readonly ISubscriptionsService _subscriptionsService;


        public SubscriptionsController(IConfiguration configuration, DBContext dBContext,
            ISubscriptionsService subscriptionsService)
        {
            _configuration = configuration;
            _dBContext = dBContext;
            _subscriptionsService = subscriptionsService;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetSubscriptionById(Guid id)
        {
            var subscriptions = await _subscriptionsService.GetSubscriptionById(id);
            return Ok(subscriptions);
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAllSubscriptions(string? range = null)
        {
            var subscriptions = await _subscriptionsService.GetAllSubscriptions(range);
            return Ok(subscriptions);
        }

        [HttpGet("allActive")]
        public async Task<IActionResult> GetAllActiveSubscriptions(string? range = null)
        {
            var subscriptions =  await _subscriptionsService.GetAllActiveSubscriptions(range);
            return Ok(subscriptions);
        }


        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSubscription(Guid id, [FromBody] SubscriptionCreateDto subscription)
        {
            var updatedSubscription = await _subscriptionsService.UpdateSubscription(id, subscription);
            return Ok(updatedSubscription);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSubscription(Guid id)
        {
            var deletedSubscription = await _subscriptionsService.DeleteSubscription(id);
            return Ok(deletedSubscription);
        }
    }
}
