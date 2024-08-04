using k8s.KubeConfigModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewsAggregation.Data;
using NewsAggregation.DTO.Ads;
using NewsAggregation.Helpers;
using NewsAggregation.Models;
using NewsAggregation.Services.Interfaces;
using User = NewsAggregation.Models.User;

namespace NewsAggregation.Services
{
    public class AdsService : IAdsService
    {

        private readonly DBContext _dBContext;
        private readonly ILogger<AuthService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AdsService(DBContext dBContext, ILogger<AuthService> logger, IHttpContextAccessor httpContextAccessor)
        {
            _dBContext = dBContext;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<IActionResult> GetAllAds(string? range = null)
        {

            var queryParams = ParameterParser.ParseRangeAndSort(range, "sort");

            try
            {
                var now = DateTime.UtcNow;

                var ads = await _dBContext.Ads
                    .Skip((queryParams.Page - 1) * queryParams.PerPage)
                    .Take(queryParams.PerPage)
                    .ToListAsync();

                return new OkObjectResult(ads);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAds");
                return new StatusCodeResult(500);
            }
            
        }

        public async Task<IActionResult> GetAllActiveAds(string? range = null)
        {
            var queryParams = ParameterParser.ParseRangeAndSort(range, "sort");

            try
            {
                var now = DateTime.UtcNow;

                var ads = await _dBContext.Ads
                    .Where(a => a.CreatedAt <= now && a.ValidUntil >= now)
                    .Skip((queryParams.Page - 1) * queryParams.PerPage)
                    .Take(queryParams.PerPage)
                    .ToListAsync();

                return new OkObjectResult(ads);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAds");
                return new StatusCodeResult(500);
            }
        }

        public async Task<IActionResult> GetAd(Guid id)
        {
            try
            {
                var ad = await _dBContext.Ads.FindAsync(id);

                if (ad == null)
                {
                    return new NotFoundResult();
                }

                return new OkObjectResult(ad);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAd");
                return new StatusCodeResult(500);
            }
        }

        public async Task<IActionResult> CreateAd(AdCreateDto adCreateDto)
        {
            try
            {

                Ads ad = new Ads();
                
                ad.CreatedAt = DateTime.UtcNow;
                ad.ValidUntil = adCreateDto.ValidUntil;
                ad.Title = adCreateDto.Title;
                ad.ImageUrl = adCreateDto.ImageUrl;
                ad.RedirectUrl = adCreateDto.RedirectUrl;
                ad.Description = adCreateDto.Description;
                ad.Clicks = 0;
                ad.Views = 0;


                _dBContext.Ads.Add(ad);
                await _dBContext.SaveChangesAsync();

                return new OkObjectResult(ad);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreateAd");
                return new StatusCodeResult(500);
            }
        }

        public async Task<IActionResult> UpdateAd(Guid id, AdCreateDto adCreateDto)
        {
            try
            {
                var ad = await _dBContext.Ads.FindAsync(id);

                if (ad == null)
                {
                    return new NotFoundResult();
                }

                ad.ValidUntil = adCreateDto.ValidUntil;
                ad.Title = adCreateDto.Title;
                ad.ImageUrl = adCreateDto.ImageUrl;
                ad.RedirectUrl = adCreateDto.RedirectUrl;
                ad.Description = adCreateDto.Description;

                await _dBContext.SaveChangesAsync();

                return new OkObjectResult(ad);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateAd");
                return new StatusCodeResult(500);
            }
        }

        public async Task<IActionResult> DeleteAd(Guid id)
        {
            try
            {
                var ad = await _dBContext.Ads.FindAsync(id);

                if (ad == null)
                {
                    return new NotFoundResult();
                }

                _dBContext.Ads.Remove(ad);
                await _dBContext.SaveChangesAsync();

                return new OkResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteAd");
                return new StatusCodeResult(500);
            }
        }

        public async Task<IActionResult> GetPersonalizedAds(string? range = null)
        {
            var queryParams = ParameterParser.ParseRangeAndSort(range, "sort");
            try
            {
                var now = DateTime.UtcNow;

                var httpContex = _httpContextAccessor.HttpContext;

                if (httpContex == null)
                {
                    return new UnauthorizedObjectResult(new { Message = "No http context found.", Code = 1000 });
                }

                var refreshToken = httpContex.Request.Cookies["refreshToken"];
                var userAgent = httpContex.Request.Headers["User-Agent"].ToString();
                var ipAddress = IP_Address.GetUserIp();

                if (refreshToken == null)
                {
                    return new UnauthorizedObjectResult(new { Message = "No refresh token found.", Code = 40 });
                }

                var user = await FindUserByRefreshToken(refreshToken, userAgent);

                if (user != null)
                {
                    var userId = user.Id;


                    var userHistory = await _dBContext.WatchHistories
                        .Where(uh => uh.UserId == userId || uh.IpAddress == ipAddress || uh.UserAgent == userAgent)
                        .ToListAsync();

                    var userBookmarks = await _dBContext.Bookmarks
                        .Where(b => b.UserId == userId)
                        .ToListAsync();

                    // Extract tags or categories of interest from user history and bookmarks
                    var userTags = userHistory.Select(uh => uh.Tags).Concat(userBookmarks.Select(b => b.Article.Tags)).Distinct().ToList();

                    // Fetch ads based on user tags and ensure they are active
                    var ads = await _dBContext.Ads
                        .Where(a => a.CreatedAt <= now && a.ValidUntil >= now && userTags.Contains(a.Tags))
                        .OrderByDescending(a => a.GuaranteedViews > a.Views) // Prioritize ads that haven't reached guaranteed views
                        .ThenBy(a => a.Views) // Order by views (ascending)
                        .ThenByDescending(a => a.Clicks) // Then by clicks (descending)
                        .ThenBy(a => a.CreatedAt) // Then by creation date (ascending)
                        .ThenBy(a => a.ValidUntil) // Then by valid until date (ascending)
                        .Skip((queryParams.Page - 1) * queryParams.PerPage)
                        .Take(queryParams.PerPage)
                        .ToListAsync();

                    return new OkObjectResult(ads);
                } 
                else
                {
                    var userHistory = await _dBContext.WatchHistories
                        .Where(uh => uh.IpAddress == ipAddress || uh.UserAgent == userAgent)
                        .ToListAsync();

                    // Extract tags or categories of interest from user history
                    var userTags = userHistory.Select(uh => uh.Tags).Distinct().ToList();

                    // Order ads by multiple fields, such as views, clicks, created at, valid until, etc.

                    var ads = await _dBContext.Ads
                        .Where(a => a.CreatedAt <= now && a.ValidUntil >= now && userTags.Contains(a.Tags))
                        .OrderByDescending(a => a.GuaranteedViews > a.Views) // Prioritize ads that haven't reached guaranteed views
                        .ThenBy(a => a.Views) // Order by views (ascending)
                        .ThenByDescending(a => a.Clicks) // Then by clicks (descending)
                        .ThenBy(a => a.CreatedAt) // Then by creation date (ascending)
                        .ThenBy(a => a.ValidUntil) // Then by valid until date (ascending)
                        .Skip((queryParams.Page - 1) * queryParams.PerPage)
                        .Take(queryParams.PerPage)
                        .ToListAsync();

                    return new OkObjectResult(ads);
                }

                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetPersonalizedAds");
                return new StatusCodeResult(500);
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
