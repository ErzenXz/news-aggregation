using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewsAggregation.Data;
using NewsAggregation.DTO.Payments;
using NewsAggregation.Services.Interfaces;

namespace NewsAggregation.Controllers
{
    [Route("payment")]
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


        [HttpGet("{id}"), Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> GetAllActivePayments(Guid id)
        {
            var payment = await _paymentService.GetPaymentById(id);
            return Ok(payment);
        }

        [HttpGet("all"), Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> GetAllPayments(string? range = null)
        {
            var payments = await _paymentService.GetAllPayments(range);
            return Ok(payments);
        }


        [HttpPost("create"), AllowAnonymous]
        public async Task<IActionResult> CreatePayment([FromBody] PaymentCreateDto payment)
        {
            var newPayment = await _paymentService.CreatePayment(payment);
            return Ok(newPayment);
        }

    }
}
