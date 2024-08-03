using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using News_aggregation.Entities;
using NewsAggregation.Data;
using NewsAggregation.DTO;
using NewsAggregation.DTO.Article;
using NewsAggregation.Helpers;
using NewsAggregation.Models;
using NewsAggregation.Services.Interfaces;
using Stripe.Forwarding;

namespace NewsAggregation.Services
{
    public class UserService : IUserService
    {


        private readonly DBContext _dBContext;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;
        private readonly ILogger<UserService> _logger;

        public UserService(DBContext dbContext, IHttpContextAccessor httpContextAccessor, IConfiguration configuration, ILogger<UserService> logger)
        {
            _dBContext = dbContext;
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
            _logger = logger;
        }


        public async Task<IActionResult> GetActiveSessions()
        {

            var httpContex = _httpContextAccessor.HttpContext;


            var refreshToken = httpContex.Request.Cookies["refreshToken"];
            var userAgent = httpContex.Request.Headers["User-Agent"].ToString();

            if (refreshToken == null)
            {
                return new UnauthorizedObjectResult(new { Message = "Unauthorized to perform this request.", Code = 76 });
            }

            var user = FindUserByRefreshToken(refreshToken, userAgent);

            if (user == null)
            {
                return new UnauthorizedObjectResult(new { Message = "Unauthorized to perform this request.", Code = 76 });
            }

            var currentTime = DateTime.UtcNow;

            // Find all refresh tokens for the user
            var refreshTokens = _dBContext.refreshTokens.Where(r => r.UserId == user.Id).ToList();

            var currentRefreshTokenVersion = user.TokenVersion;

            var activeSessions = new List<ActiveSession>();

            // Check if any of the refresh tokens are still active

            foreach (var token in refreshTokens)
            {
                if (currentRefreshTokenVersion == token.TokenVersion && token.IsActive)
                {
                    // If the token is active, add it to the list
                    activeSessions.Add(new ActiveSession
                    {
                        Id = token.Id,
                        CreatedAt = token.Created,
                        Expires = token.Expires,
                        UserAgent = token.UserAgent,
                        IpAddress = token.CreatedByIp,
                        IsActive = token.IsActive
                    });
                }
            }

            // Add Content-Range header
            httpContex.Response.Headers.Add(HeaderNames.ContentRange, $"activeSessions 0-{activeSessions.Count - 1}/{activeSessions.Count}");

            return new OkObjectResult(activeSessions);
        }

        public async Task<IActionResult> GetSavedArticles(string? range = null)
        {
            var httpContex = _httpContextAccessor.HttpContext;
            var refreshToken = httpContex.Request.Cookies["refreshToken"];
            var userAgent = httpContex.Request.Headers["User-Agent"].ToString();

            if (refreshToken == null)
            {
                return new UnauthorizedObjectResult(new { Message = "Unauthorized to perform this request.", Code = 76 });
            }

            var user = FindUserByRefreshToken(refreshToken, userAgent);

            if (user == null)
            {
                return new UnauthorizedObjectResult(new { Message = "Unauthorized to perform this request.", Code = 76 });
            }
            var queryParams = ParameterParser.ParseRangeAndSort(range, "sort");
            var page = queryParams.Page;
            var pageSize = queryParams.PerPage;

            var savedArticles = await _dBContext.Bookmarks.Where(s => s.UserId == user.Id).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            var articles = new List<Article>();

            foreach (var savedArticle in savedArticles)
            {
                var article = _dBContext.Articles.FirstOrDefault(a => a.Id == savedArticle.ArticleId);
                if (article != null)
                {
                    articles.Add(article);
                }
            }

            return new OkObjectResult(savedArticles);
        }

        public async Task<IActionResult> GetViewHistory(string? range = null)
        {
            var httpContex = _httpContextAccessor.HttpContext;
            var refreshToken = httpContex.Request.Cookies["refreshToken"];
            var userAgent = httpContex.Request.Headers["User-Agent"].ToString();

            if (refreshToken == null)
            {
                return new UnauthorizedObjectResult(new { Message = "Unauthorized to perform this request.", Code = 76 });
            }

            var user = FindUserByRefreshToken(refreshToken, userAgent);

            if (user == null)
            {
                return new UnauthorizedObjectResult(new { Message = "Unauthorized to perform this request.", Code = 76 });
            }

            var queryParams = ParameterParser.ParseRangeAndSort(range, "sort");
            var page = queryParams.Page;
            var pageSize = queryParams.PerPage;

            var watchHistory = await _dBContext.WatchHistories.Where(s => s.UserId == user.Id).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            var articles = new List<ArticleDto>();

            foreach (var history in watchHistory)
            {
                var article = await _dBContext.Articles.FirstOrDefaultAsync(a => a.Id == history.ArticleId);
                if (article != null)
                {
                    var artDTO = new ArticleDto {Title = article.Title, Content = article.Description, ImageUrl = article.ImageUrl, CategoryId = article.CategoryId, Tags = article.Tags, PublishedAt = article.CreatedAt };


                    // Check if it exists alerdy

                    if (articles.Any(a => a.Title == artDTO.Title))
                    {
                        continue;
                    }

                    articles.Add(artDTO);
                }
            }

            return new OkObjectResult(articles);
        }

        public async Task<IActionResult> RevokeActiveSession(string? ipAdress, string userAgent)
        {
            var httpContex = _httpContextAccessor.HttpContext;
            var refreshToken = httpContex.Request.Cookies["refreshToken"];
            var currentUserAgent = httpContex.Request.Headers["User-Agent"].ToString();

            if (refreshToken == null)
            {
                return new UnauthorizedObjectResult(new { Message = "Unauthorized to perform this request.", Code = 76 });
            }

            var user = FindUserByRefreshToken(refreshToken, currentUserAgent);

            if (user == null)
            {
                return new UnauthorizedObjectResult(new { Message = "Unauthorized to perform this request.", Code = 76 });
            }

            if (userAgent == null || ipAdress == null)
            {
                return new BadRequestObjectResult(new { Message = "User agent and IP address are required.", Code = 77 });
            }

            var token = _dBContext.refreshTokens.FirstOrDefault(r => r.UserId == user.Id && r.UserAgent == userAgent && r.CreatedByIp == ipAdress);

            if (token == null)
            {
                return new NotFoundObjectResult(new { Message = "Session not found.", Code = 78 });
            }

            token.Revoked = DateTime.UtcNow;
            token.RevocationReason = "User requested revocation.";
            token.RevokedByIp = IP_Address.GetUserIp();

            await _dBContext.SaveChangesAsync();

            return new OkObjectResult(new { Message = "Session revoked successfully.", Code = 80 });
        }


        public async Task<IActionResult> UpdateUser(UserUpdateRequest userUpdateRequest)
        {

            var httpContex = _httpContextAccessor.HttpContext;
            var refreshToken = httpContex.Request.Cookies["refreshToken"];
            var userAgent = httpContex.Request.Headers["User-Agent"].ToString();

            if (refreshToken == null)
            {
                return new UnauthorizedObjectResult(new { Message = "Unauthorized to perform this request.", Code = 76 });
            }

            var user = FindUserByRefreshToken(refreshToken, userAgent);
            if (user == null)
            {
                return new UnauthorizedObjectResult(new { Message = "Unauthorized to perform this request.", Code = 76 });
            }

            if (_dBContext.Users.Any(u => u.Username == userUpdateRequest.Username))
            {
                return new BadRequestObjectResult(new { Message = "Username already in use.", Code = 8 });
            }

            if (_dBContext.Users.Any(u => u.Email == userUpdateRequest.Email))
            {
                return new BadRequestObjectResult(new { Message = "Email already in use.", Code = 7 });
            }

            user.Username = userUpdateRequest.Username;
            user.FullName = userUpdateRequest.Fullname;
            user.Email = userUpdateRequest.Email;
            user.Birthdate = userUpdateRequest.Birthday;
            user.TimeZone = userUpdateRequest.TimeZone;
            user.Language = userUpdateRequest.Language;

            await _dBContext.SaveChangesAsync();


            return new OkObjectResult(new { Message = "User updated successfully.", Code = 79 });
        }

        public async Task<IActionResult> UpdateUserBirthdate(DateTime newBirthDay)
        {
            var httpContex = _httpContextAccessor.HttpContext;
            var refreshToken = httpContex.Request.Cookies["refreshToken"];
            var userAgent = httpContex.Request.Headers["User-Agent"].ToString();
            
            if (refreshToken == null)
            {
                return new UnauthorizedObjectResult(new { Message = "Unauthorized to perform this request.", Code = 76 });
            }
            var user = FindUserByRefreshToken(refreshToken, userAgent);


            if (user == null)
            {
                return new UnauthorizedObjectResult(new { Message = "Unauthorized to perform this request.", Code = 76 });
            }

            // Check if the new birthdate is valid
            if (newBirthDay > DateTime.UtcNow)
            {
                return new BadRequestObjectResult(new { Message = "Invalid birthdate.", Code = 9 });
            }

            if (newBirthDay == user.Birthdate)
            {
                return new BadRequestObjectResult(new { Message = "Birthdate is the same as the current one.", Code = 15 });
            }

            user.Birthdate = newBirthDay;
            await _dBContext.SaveChangesAsync();


            return new OkObjectResult(new { Message = "Birthdate updated successfully.", Code = 84 });
        }

        public async Task<IActionResult> UpdateUserUsername(string newUsername)
        {
            var httpContex = _httpContextAccessor.HttpContext;
            var refreshToken = httpContex.Request.Cookies["refreshToken"];
            var userAgent = httpContex.Request.Headers["User-Agent"].ToString();
            
            if (refreshToken == null)
            {
                return new UnauthorizedObjectResult(new { Message = "Unauthorized to perform this request.", Code = 76 });
            }
            var user = FindUserByRefreshToken(refreshToken, userAgent);
            if (user == null)
            {
                return new UnauthorizedObjectResult(new { Message = "Unauthorized to perform this request.", Code = 76 });
            }

            if (newUsername == null)
            {
                return new BadRequestObjectResult(new { Message = "Username cannot be empty.", Code = 12 });
            }

            if (_dBContext.Users.Any(u => u.Username == newUsername))
            {
                return new BadRequestObjectResult(new { Message = "Username already in use.", Code = 8 });
            }

            user.Username = newUsername;
            await _dBContext.SaveChangesAsync();

            return new OkObjectResult(new { Message = "Username updated successfully.", Code = 82 });

        }

        public async Task<IActionResult> UpdateUserFullName(string newName)
        {
            var httpContex = _httpContextAccessor.HttpContext;
            var refreshToken = httpContex.Request.Cookies["refreshToken"];
            var userAgent = httpContex.Request.Headers["User-Agent"].ToString();

            if (refreshToken == null)
            {
                return new UnauthorizedObjectResult(new { Message = "Unauthorized to perform this request.", Code = 76 });
            }
            var user = FindUserByRefreshToken(refreshToken, userAgent);
            if (user == null)
            {
                return new BadRequestObjectResult(new { Message = "User not found.", Code = 36 });
            }

            if (newName == null)
            {
                return new BadRequestObjectResult(new { Message = "Full name cannot be empty.", Code = 16 });
            }

            user.FullName = newName;
            await _dBContext.SaveChangesAsync();


            return new OkObjectResult(new { Message = "Full name updated successfully.", Code = 83 });
        }

        public async Task<IActionResult> UpdateProfileImage(string imageUrl)
        {
            var httpContex = _httpContextAccessor.HttpContext;
            var refreshToken = httpContex.Request.Cookies["refreshToken"];
            var userAgent = httpContex.Request.Headers["User-Agent"].ToString();

            if (refreshToken == null)
            {
                return new UnauthorizedObjectResult(new { Message = "Unauthorized to perform this request.", Code = 76 });
            }
            var user = FindUserByRefreshToken(refreshToken, userAgent);
            if (user == null)
            {
                return new BadRequestObjectResult(new { Message = "User not found.", Code = 36 });
            }

            if (imageUrl == null)
            {
                return new BadRequestObjectResult(new { Message = "Image URL cannot be empty.", Code = 1000 });
            }

            user.ProfilePicture = imageUrl;
            await _dBContext.SaveChangesAsync();
            return new OkObjectResult(new { Message = "Profile image updated successfully.", Code = 1000 });
        }



        public User? FindUserByRefreshToken(string refreshToken, string userAgent)
        {
            var currentTime = DateTime.UtcNow;

            var user = _dBContext.refreshTokens.FirstOrDefault(r => r.Token == refreshToken && r.Expires > currentTime && r.UserAgent == userAgent);

            if (user == null)
            {
                return null;
            }

            return _dBContext.Users.FirstOrDefault(u => u.Id == user.UserId);

        }
    }
}
