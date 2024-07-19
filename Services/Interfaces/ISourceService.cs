using Microsoft.AspNetCore.Mvc;
using NewsAggregation.DTO.Source;
using ServiceStack;

namespace NewsAggregation.Services.Interfaces
{
    public interface ISourceService : IService
    {
        public Task<IActionResult> GetSourceById(Guid id);
        public Task<IActionResult> GetAllSources(string? range = null);
        public Task<IActionResult> CreateSource(SourceCreateDto source);
        public Task<IActionResult> UpdateSource(Guid id, SourceCreateDto source);
        public Task<IActionResult> DeleteSource(Guid id);

    }
}
