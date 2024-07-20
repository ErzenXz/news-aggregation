using Microsoft.AspNetCore.Mvc;
using NewsAggregation.Services;

namespace NewsAggregation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RssController : ControllerBase
    {
        private readonly RssService _rssFeedService;

        public RssController(RssService rssFeedService)
        {
            _rssFeedService = rssFeedService;
        }

        [HttpGet]
        public IActionResult Get(string url)
        {
            var items = _rssFeedService.ParseRssFeed(url);
            return Ok(items);
        }
    }
}
