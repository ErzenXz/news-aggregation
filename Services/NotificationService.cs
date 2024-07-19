using Microsoft.AspNetCore.Mvc;
using NewsAggregation.Data;
using NewsAggregation.Models;
using NewsAggregation.Services.Interfaces;

namespace NewsAggregation.Services
{
    public class NotificationService : INotificationService
    {

        private readonly DBContext _dBContext;
        private readonly ILogger<AuthService> _logger;

        public NotificationService(DBContext dBContext, ILogger<AuthService> logger)
        {
            _dBContext = dBContext;
            _logger = logger;
        }

        public async Task<IActionResult> SendNotification(string? title = "null", string? message = "null")
        {
            Notification notification = new()
            {
                Title = title,
                Content = message,
                CreatedAt = DateTime.UtcNow,
                UserId = new Guid("46611d66-cbd8-4a2f-9f9c-527edeab3984")
            };

            try
            {
                await _dBContext.Notifications.AddAsync(notification);
                await _dBContext.SaveChangesAsync();

                return new OkObjectResult(notification);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SendNotification");
                return new BadRequestObjectResult(new { Message = "Error notification " + ex.Message, Code = 1000 });
            }

        }
    }
}
