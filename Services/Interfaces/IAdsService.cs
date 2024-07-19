using Microsoft.AspNetCore.Mvc;
using NewsAggregation.DTO.Ads;
using ServiceStack;

namespace NewsAggregation.Services.Interfaces
{
    public interface IAdsService : IService
    {

        public Task<IActionResult> GetAd(Guid id);
        public Task<IActionResult> GetAllAds(string? range = null);
        public Task<IActionResult> GetAllActiveAds(string? range = null);
        public Task<IActionResult> CreateAd(AdCreateDto adRequest);
        public Task<IActionResult> UpdateAd(Guid id, AdCreateDto adRequest);
        public Task<IActionResult> DeleteAd(Guid id);

    }
}
