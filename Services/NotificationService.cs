using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewsAggregation.Data;
using NewsAggregation.Models;
using NewsAggregation.Services.Interfaces;

namespace NewsAggregation.Services
{
    public class NotificationService : INotificationService
    {

        private readonly DBContext _dBContext;
        private readonly ILogger<AuthService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public NotificationService(DBContext dBContext, ILogger<AuthService> logger, IHttpContextAccessor httpContextAccessor)
        {
            _dBContext = dBContext;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<IActionResult> SendNotification(string? title = "null", string? message = "null")
        {

            var httpContex = _httpContextAccessor.HttpContext;

            if (httpContex == null)
            {
                return new UnauthorizedObjectResult(new { Message = "No http context found.", Code = 1000 });
            }

            var refreshToken = httpContex.Request.Cookies["refreshToken"];
            var userAgent = httpContex.Request.Headers["User-Agent"].ToString();

            if (refreshToken == null)
            {
                return new UnauthorizedObjectResult(new { Message = "No refresh token found.", Code = 40 });
            }

            var user = await FindUserByRefreshToken(refreshToken, userAgent);

            if (user == null)
            {
                return new UnauthorizedObjectResult(new { Message = "User not found.", Code = 40 });
            }

            Notification notification = new()
            {
                Title = title,
                Content = message,
                CreatedAt = DateTime.UtcNow,
                UserId = user.Id
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

        public async Task<User?> FindUserByRefreshToken(string refreshToken, string userAgent)
        {
            var currentTime = DateTime.UtcNow;

            var refreshTokenEntry = _dBContext.refreshTokens.FirstOrDefault(r => r.Token == refreshToken && r.Expires > currentTime && r.UserAgent == userAgent && r.Revoked == null);

            if (refreshTokenEntry == null)
            {
                return null;
            }

            var userId = refreshTokenEntry.UserId;
            var refreshTokenVersion = refreshTokenEntry.TokenVersion;

            var user = await _dBContext.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return null;
            }

            if (user.TokenVersion != refreshTokenVersion)
            {
                return null;
            }

            return user;
        }
    }
}
