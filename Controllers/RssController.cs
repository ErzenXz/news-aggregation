using Microsoft.AspNetCore.Mvc;
using NewsAggregation.Services;

namespace NewsAggregation.Controllers
{
    [ApiController]
    [Route("rss")]
    public class RssController : ControllerBase
    {
        private readonly RssService _rssFeedService;

        public RssController(RssService rssFeedService)
        {
            _rssFeedService = rssFeedService;
        }

        [HttpGet]
        public async Task<IActionResult> Get(string url)
        {
            var items = await RssService.ParseRssFeed(url);
            return Ok(items);
        }
    }
}
