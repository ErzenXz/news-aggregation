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
using NewsAggregation.DTO.Subscriptions;
using SubscriptionT = NewsAggregation.Models.Subscriptions;

namespace NewsAggregation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StripeController : ControllerBase
    {
        private readonly ILogger<StripeController> _logger;
        private readonly DBContext _dbContext;
        private readonly string _webhookSecret;

        public StripeController(ILogger<StripeController> logger, DBContext dbContext, IConfiguration configuration)
        {
            _logger = logger;
            _dbContext = dbContext;
            _webhookSecret = configuration["Stripe:WebhookSecret"];
        }

        [HttpPost("webhook")]
        public async Task<IActionResult> HandleStripeWebhook()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

            try
            {
                var stripeEvent = EventUtility.ConstructEvent(json, Request.Headers["Stripe-Signature"], _webhookSecret);

                if (stripeEvent == null)
                {
                    _logger.LogWarning("Failed to construct event from payload");
                    return BadRequest();
                }

                switch (stripeEvent.Type)
                {
                    case Events.InvoicePaymentSucceeded:
                        var invoice = stripeEvent.Data.Object as Invoice;
                        await HandleInvoicePaymentSucceeded(invoice);
                        break;

                    case Events.InvoicePaymentFailed:
                        var failedInvoice = stripeEvent.Data.Object as Invoice;
                        await HandleInvoicePaymentFailed(failedInvoice);
                        break;

                    case Events.CustomerSubscriptionDeleted:
                        var deletedSubscription = stripeEvent.Data.Object as Subscription;
                        await HandleCustomerSubscriptionDeleted(deletedSubscription);
                        break;

                    case Events.CustomerSubscriptionUpdated:
                        var updatedSubscription = stripeEvent.Data.Object as Subscription;
                        await HandleSubscriptionUpdated(updatedSubscription);
                        break;


                    default:
                        _logger.LogInformation($"Unhandled event type: {stripeEvent.Type}");
                        break;
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
                var subscriptionId = invoice.SubscriptionId;

            // Check if the subscription exists in the database

            var subscription = await _dbContext.Subscriptions.FirstOrDefaultAsync(s => s.StripeCustomerId == invoice.CustomerId);

            if (subscription == null)
            {
                var subscrip = new SubscriptionT
                {
                    UserId = invoice.CustomerId,
                    PlanId = invoice.SubscriptionId,
                    StartDate = invoice.Created,
                    EndDate = invoice.DueDate,
                    Amount = invoice.Subtotal,
                    Currency = invoice.Currency
                };


                subscrip.IsPaid = true;
                subscrip.IsActive = true;

                await _dbContext.Subscriptions.AddAsync(subscrip);

                await _dbContext.SaveChangesAsync();

                _logger.LogInformation($"Subscription created: {invoice.SubscriptionId}");

                }
            else
            {
                subscription.IsPaid = true;
                subscription.IsActive = true;
                subscription.LastPaymentDate = invoice.Created;

                _dbContext.Subscriptions.Update(subscription);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation($"Invoice payment succeeded for subscription {subscriptionId} update");
            }


            _logger.LogInformation($"Invoice payment succeeded for subscription {subscriptionId}");
        }


        private async Task HandleInvoicePaymentFailed(Invoice invoice)
        {
            var subscriptionId = invoice.SubscriptionId;
            var subscription = await _dbContext.Subscriptions.FirstOrDefaultAsync(s => s.StripeSubscriptionId == subscriptionId);

            if (subscription != null)
            {
                subscription.IsPaid = false;

                _dbContext.Subscriptions.Update(subscription);
                await _dbContext.SaveChangesAsync();
            }

            _logger.LogWarning($"Invoice payment failed for subscription {subscriptionId}");
        }

        private async Task HandleCustomerSubscriptionDeleted(Subscription stripeSubscription)
        {
            var subscription = await _dbContext.Subscriptions.FirstOrDefaultAsync(s => s.StripeSubscriptionId == stripeSubscription.Id);

            if (subscription != null)
            {
                subscription.IsActive = false;
                subscription.EndDate = stripeSubscription.CurrentPeriodEnd;

                _dbContext.Subscriptions.Update(subscription);
                await _dbContext.SaveChangesAsync();
            }

            _logger.LogInformation($"Subscription deleted: {stripeSubscription.Id}");
        }

        private async Task HandleSubscriptionUpdated(Subscription stripeSubscription)
        {
            var subscription = await _dbContext.Subscriptions.FirstOrDefaultAsync(s => s.StripeSubscriptionId == stripeSubscription.Id);

            if (subscription != null)
            {
                subscription.EndDate = stripeSubscription.CurrentPeriodEnd;
                subscription.IsActive = stripeSubscription.Status == "active";

                _dbContext.Subscriptions.Update(subscription);
                await _dbContext.SaveChangesAsync();
            }

            _logger.LogInformation($"Subscription updated: {stripeSubscription.Id}");
        }
    }

}
