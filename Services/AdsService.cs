using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewsAggregation.Data;
using NewsAggregation.DTO.Ads;
using NewsAggregation.Models;
using NewsAggregation.Services.Interfaces;

namespace NewsAggregation.Services
{
    public class AdsService : IAdsService
    {

        private readonly DBContext _dBContext;
        private readonly ILogger<AuthService> _logger;

        public AdsService(DBContext dBContext, ILogger<AuthService> logger)
        {
            _dBContext = dBContext;
            _logger = logger;
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

    }
}
