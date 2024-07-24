using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewsAggregation.Data;
using NewsAggregation.DTO.Payments;
using NewsAggregation.Models;
using NewsAggregation.Services.Interfaces;
using Stripe.Checkout;

namespace NewsAggregation.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly DBContext _dBContext;
        private readonly ILogger<AuthService> _logger;

        public PaymentService(DBContext dBContext, ILogger<AuthService> logger)
        {
            _dBContext = dBContext;
            _logger = logger;
        }

        public async Task<IActionResult> GetAllPayments(string? range = null)
        {
            var queryParams = ParameterParser.ParseRangeAndSort(range, "sort");

            try
            {
                var payments = await _dBContext.Payments
                    .Skip((queryParams.Page - 1) * queryParams.PerPage)
                    .Take(queryParams.PerPage)
                    .ToListAsync();

                return new OkObjectResult(payments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetPayments");
                return new StatusCodeResult(500);
            }
        }

        public async Task<IActionResult> GetPaymentById(Guid id)
        {
            try
            {
                var payment = await _dBContext.Payments
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (payment == null)
                {
                    return new NotFoundResult();
                }

                return new OkObjectResult(payment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetPaymentById");
                return new StatusCodeResult(500);
            }
        }

        public async Task<IActionResult> CreatePayment(PaymentCreateDto paymentRequest)
        {
            try
            {
                var options = new SessionCreateOptions
                {
                    LineItems = new List<SessionLineItemOptions>
                    {
                        new SessionLineItemOptions
                        {
                            PriceData = new SessionLineItemPriceDataOptions
                            {
                                UnitAmount = (long)(paymentRequest.Amount * 100), // Amount in cents
                                Currency = paymentRequest.Currency,
                                ProductData = new SessionLineItemPriceDataProductDataOptions
                                {
                                    Name = paymentRequest.PaymentDescription,
                                },
                            },
                            Quantity = 1,
                        },
                    },
                    Mode = "payment",
                    SuccessUrl = "http://localhost:5173/success", // Placeholder URL
                    CancelUrl = "http://localhost:5173/cancel", // Placeholder URL
                };

                var service = new SessionService();
                Session session = await service.CreateAsync(options);
                
                var payment = new Payment
                {
                    Id = Guid.NewGuid(),
                    SubscriptionId = paymentRequest.SubscriptionId,
                    Amount = paymentRequest.Amount,
                    Currency = paymentRequest.Currency,
                    PaymentMethod = paymentRequest.PaymentMethod,
                    PaymentStatus = paymentRequest.PaymentStatus,
                    PaymentGateway = paymentRequest.PaymentGateway,
                    PaymentReference = paymentRequest.PaymentReference,
                    PaymentDescription = paymentRequest.PaymentDescription,
                    PaymentDate = paymentRequest.PaymentDate
                };

                await _dBContext.Payments.AddAsync(payment);
                await _dBContext.SaveChangesAsync();

                Console.WriteLine($"Payment URL: {session.Url}");
                return new OkObjectResult(new { url = session.Url });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreatePayment");
                return new StatusCodeResult(500);
            }
        }

        public async Task<IActionResult> UpdatePayment(Guid id, PaymentCreateDto paymentRequest)
        {
            try
            {
                var payment = await _dBContext.Payments
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (payment == null)
                {
                    return new NotFoundResult();
                }

                payment.SubscriptionId = paymentRequest.SubscriptionId;
                payment.Amount = paymentRequest.Amount;
                payment.Currency = paymentRequest.Currency;
                payment.PaymentMethod = paymentRequest.PaymentMethod;
                payment.PaymentStatus = paymentRequest.PaymentStatus;
                payment.PaymentGateway = paymentRequest.PaymentGateway;
                payment.PaymentReference = paymentRequest.PaymentReference;
                payment.PaymentDescription = paymentRequest.PaymentDescription;
                payment.PaymentDate = paymentRequest.PaymentDate;

                await _dBContext.SaveChangesAsync();

                return new OkObjectResult(payment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdatePayment");
                return new StatusCodeResult(500);
            }
        }

        public async Task<IActionResult> DeletePayment(Guid id)
        {
            try
            {
                var payment = await _dBContext.Payments
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (payment == null)
                {
                    return new NotFoundResult();
                }

                _dBContext.Payments.Remove(payment);
                await _dBContext.SaveChangesAsync();

                return new OkObjectResult(payment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeletePayment");
                return new StatusCodeResult(500);
            }
        }
        
        // public async Task<IActionResult> CreateStripePayment(PaymentCreateDto paymentRequest)
        // {
        //     try
        //     {
        //         var options = new SessionCreateOptions
        //         {
        //             LineItems = new List<SessionLineItemOptions>
        //             {
        //                 new SessionLineItemOptions
        //                 {
        //                     PriceData = new SessionLineItemPriceDataOptions
        //                     {
        //                         UnitAmount = (long)(paymentRequest.Amount * 100), // Amount in cents
        //                         Currency = paymentRequest.Currency,
        //                         ProductData = new SessionLineItemPriceDataProductDataOptions
        //                         {
        //                             Name = paymentRequest.PaymentDescription,
        //                         },
        //                     },
        //                     Quantity = 1,
        //                 },
        //             },
        //             Mode = "payment",
        //             SuccessUrl = "http://localhost:5173/success", // Placeholder URL
        //             CancelUrl = "http://localhost:5173/cancel", // Placeholder URL
        //         };
        //
        //         var service = new SessionService();
        //         Session session = await service.CreateAsync(options);
        //
        //         var payment = new Payment
        //         {
        //             Id = Guid.NewGuid(),
        //             SubscriptionId = paymentRequest.SubscriptionId,
        //             Amount = paymentRequest.Amount,
        //             Currency = paymentRequest.Currency,
        //             PaymentMethod = paymentRequest.PaymentMethod,
        //             PaymentStatus = "Pending",
        //             PaymentGateway = "Stripe",
        //             PaymentReference = session.Id,
        //             PaymentDescription = paymentRequest.PaymentDescription,
        //             PaymentDate = DateTime.UtcNow
        //         };
        //
        //         await _dBContext.Payments.AddAsync(payment);
        //         await _dBContext.SaveChangesAsync();
        //
        //         // Output the URL for manual testing
        //         Console.WriteLine($"Payment URL: {session.Url}");
        //         return new OkObjectResult(new { url = session.Url });
        //     }
        //     catch (Exception ex)
        //     {
        //         _logger.LogError(ex, "Error in CreateStripePayment");
        //         return new StatusCodeResult(500);
        //     }
        // }


    }
}
