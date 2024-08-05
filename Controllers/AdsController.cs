using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewsAggregation.Data;
using NewsAggregation.DTO.Ads;
using NewsAggregation.Services.Interfaces;

namespace NewsAggregation.Controllers
{
    [Route("ads")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ApiController]
    public class AdsController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly DBContext _dBContext;
        private readonly IAdsService _adsService;


        public AdsController(IConfiguration configuration, DBContext dBContext, IAdsService adsService)
        {
            _configuration = configuration;
            _dBContext = dBContext;
            _adsService = adsService;
        }


        [HttpGet("{id}")]
        public async Task<IActionResult> GetAd(Guid id)
        {
            var response = await _adsService.GetAd(id);

            return response;

        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAllAds(string? range = null)
        {
            var response = await _adsService.GetAllAds(range);
 
            return response;

        }

        [HttpGet("allActive")]
        public async Task<IActionResult> GetAllActiveAds(string? range = null)
        {
            var response = await _adsService.GetAllActiveAds(range);
            return response;

        }

        [HttpGet("personalized"), AllowAnonymous]
        public async Task<IActionResult> GetPersonalizedAds(string? range = null)
        {
            var response = await _adsService.GetPersonalizedAds(range);
            return response;

        }


        [HttpPost("create")]
        public async Task<IActionResult> CreateAd(AdCreateDto adRequest)
        {
            var response = await _adsService.CreateAd(adRequest);
            return response;

        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAd(Guid id, AdCreateDto adRequest)
        {
            var response = await _adsService.UpdateAd(id, adRequest);

            return response;
  
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAd(Guid id)
        {
            var response = await _adsService.DeleteAd(id);

            return response;

        }
    }
}
