using Microsoft.AspNetCore.Mvc;
using NewsAggregation.DTO.Payments;
using ServiceStack;

namespace NewsAggregation.Services.Interfaces
{
    public interface IPaymentService : IService
    {
        public Task<IActionResult> GetPaymentById(Guid id);
        public Task<IActionResult> GetAllPayments(string? range = null);
        public Task<IActionResult> CreatePayment(PaymentCreateDto paymentRequest);
        //public Task<IActionResult> UpdatePayment(Guid id, PaymentCreateDto paymentRequest);
        //public Task<IActionResult> DeletePayment(Guid id);
        //public Task<IActionResult> CreateStripePayment(PaymentCreateDto paymentRequest);

    }
}
