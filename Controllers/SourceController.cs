using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewsAggregation.Data;
using NewsAggregation.DTO.Source;
using NewsAggregation.Services.Interfaces;

namespace NewsAggregation.Controllers
{
    [Route("source")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ApiController]
    public class SourceController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly DBContext _dBContext;
        private readonly ISourceService _sourceService;


        public SourceController(IConfiguration configuration, DBContext dBContext,
            ISourceService sourceService)
        {
            _configuration = configuration;
            _dBContext = dBContext;
            _sourceService = sourceService;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetSourceById(Guid id)
        {
            return await _sourceService.GetSourceById(id);
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAllSources([FromQuery] string? range = null)
        {
            return await _sourceService.GetAllSources(range);
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateSource([FromBody] SourceCreateDto createSourceDTO)
        {
            return await _sourceService.CreateSource(createSourceDTO);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSource(Guid id, [FromBody] SourceCreateDto updateSourceDTO)
        {
            return await _sourceService.UpdateSource(id, updateSourceDTO);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSource(Guid id)
        {
            return await _sourceService.DeleteSource(id);
        }
    }
}
