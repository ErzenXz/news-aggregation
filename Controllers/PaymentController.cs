using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewsAggregation.Data;
using NewsAggregation.DTO.Payments;
using NewsAggregation.Services.Interfaces;

namespace NewsAggregation.Controllers
{
    [Route("payment")]
    [Authorize(Roles = "User,Admin,SuperAdmin")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly DBContext _dBContext;
        private readonly IPaymentService _paymentService;


        public PaymentController(IConfiguration configuration, DBContext dBContext,
            IPaymentService paymentService)
        {
            _configuration = configuration;
            _dBContext = dBContext;
            _paymentService = paymentService;
        }


        [HttpGet("{id}")]
        public async Task<IActionResult> GetAllActivePayments(Guid id)
        {
            var payment = await _paymentService.GetPaymentById(id);
            return Ok(payment);
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAllPayments(string? range = null)
        {
            var payments = await _paymentService.GetAllPayments(range);
            return Ok(payments);
        }


        [HttpPost("create")]
        public async Task<IActionResult> CreatePayment([FromBody] PaymentCreateDto payment)
        {
            var newPayment = await _paymentService.CreatePayment(payment);
            return Ok(newPayment);
        }
        /*
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePayment(Guid id, [FromBody] PaymentCreateDto payment)
        {
            var updatedPayment = await _paymentService.UpdatePayment(id, payment);
            return Ok(updatedPayment);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePayment(Guid id)
        {
            var deletedPayment = await _paymentService.DeletePayment(id);
            return Ok(deletedPayment);
        }
        */
    }
}
