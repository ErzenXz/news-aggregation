using Microsoft.AspNetCore.Mvc;
using NewsAggregation.Data;
using NewsAggregation.Models;
using Stripe;
using Stripe.Checkout;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace NewsAggregation.Controllers
{
    [Route("api/stripe")]
    [ApiController]
    public class StripeWebhookController : ControllerBase
    {
        private readonly string _webhookSecret;
        private readonly DBContext _dbContext;
        private readonly ILogger<StripeWebhookController> _logger;

        public StripeWebhookController(IConfiguration configuration, DBContext dbContext, ILogger<StripeWebhookController> logger)
        {
            _webhookSecret = configuration["Stripe:WebhookSecret"];
            _dbContext = dbContext;
            _logger = logger;
        }

        [HttpPost("webhook")]
        public async Task<IActionResult> HandleStripeWebhook()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

            try
            {
                var stripeEvent = EventUtility.ConstructEvent(json, Request.Headers["Stripe-Signature"], _webhookSecret);

                if (stripeEvent.Type == Events.InvoicePaymentSucceeded)
                {
                    var invoice = stripeEvent.Data.Object as Invoice;
                    await HandleInvoicePaymentSucceeded(invoice);
                }
                else if (stripeEvent.Type == Events.CustomerSubscriptionDeleted)
                {
                    var subscription = stripeEvent.Data.Object as Subscription;
                    await HandleCustomerSubscriptionDeleted(subscription);
                }
                else if (stripeEvent.Type == Events.SubscriptionScheduleCanceled)
                {
                    var canceled = stripeEvent.Data.Object as Subscription;
                }

                return Ok();
            }
            catch (StripeException ex)
            {
                _logger.LogError($"Stripe webhook error: {ex.Message}");
                return BadRequest();
            }
        }

        private async Task HandleInvoicePaymentSucceeded(Invoice invoice)
        {
            // Find the corresponding subscription
            var subscriptionId = invoice.SubscriptionId;
            var subscription = await _dbContext.Subscriptions.FirstOrDefaultAsync(s => s.StripeSubscriptionId == subscriptionId);

            if (subscription != null)
            {
                // Update the subscription payment status or any other necessary fields
                subscription.LastPaymentDate = invoice.Created;
                subscription.IsPaid = true;

                _dbContext.Subscriptions.Update(subscription);
                await _dbContext.SaveChangesAsync();
            }

            _logger.LogInformation($"Invoice payment succeeded for subscription {subscriptionId}");
        }

        private async Task HandleCustomerSubscriptionDeleted(Subscription stripeSubscription)
        {
            // Find the corresponding subscription in your database
            var subscription = await _dbContext.Subscriptions.FirstOrDefaultAsync(s => s.StripeSubscriptionId == stripeSubscription.Id);

            if (subscription != null)
            {
                // Update the subscription status or any other necessary fields
                subscription.IsActive = false;
                subscription.EndDate = stripeSubscription.CurrentPeriodEnd;

                _dbContext.Subscriptions.Update(subscription);
                await _dbContext.SaveChangesAsync();
            }

            _logger.LogInformation($"Subscription deleted: {stripeSubscription.Id}");
        }
    }
}
