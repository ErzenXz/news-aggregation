using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewsAggregation.Data;
using NewsAggregation.DTO.Ads;
using NewsAggregation.Services.Interfaces;

namespace NewsAggregation.Controllers
{


    [Route("notification")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ApiController]
    public class NotificationController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly DBContext _dBContext;
        private readonly INotificationService _notificationService;


        public NotificationController(IConfiguration configuration, DBContext dBContext, INotificationService notificationService)
        {
            _configuration = configuration;
            _dBContext = dBContext;
            _notificationService = notificationService;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateNotification(string? title, string? message)
        {
            var response = await _notificationService.SendNotification(title, message);
            return response;
        }

    }
}
