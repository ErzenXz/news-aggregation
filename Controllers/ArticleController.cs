using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewsAggregation.DTO.Article;
using NewsAggregation.Services;
using NewsAggregation.Services.Interfaces;
using System;
using System.Threading.Tasks;

namespace NewsAggregation.Controllers
{
    [ApiController]
    [Route("article")]
    public class ArticleController : ControllerBase
    {
        private readonly IArticleService _articleService;

        public ArticleController(IArticleService articleService)
        {
            _articleService = articleService;
        }

        [Authorize(Roles = "Admin,SuperAdmin")]
        [HttpPost("create")]
        public async Task<ActionResult<ArticleCreateDto>> CreateArticle([FromBody] ArticleCreateDto createArticle)
        {
            var createdArticle = await _articleService.CreateArticle(createArticle);
            return Ok(createdArticle);
        }

        [Authorize(Roles = "Admin,SuperAdmin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteArticle(Guid id)
        {
            await _articleService.DeleteArticle(id);
            return NoContent();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ArticleCreateDto>> GetArticleById(Guid id)
        {
            var article = await _articleService.GetArticleById(id);
            return Ok(article);
        }

        [AllowAnonymous]
        [HttpGet("all")]
        public async Task<IActionResult> GetAllArticles(string? range = null)
        {
            var articles = await _articleService.GetAllArticles(range);

            if (articles is OkObjectResult okResult)
            {
                return Ok(okResult.Value);
            }
            else if (articles is NotFoundResult)
            {
                return NotFound();
            }
            else if (articles is BadRequestObjectResult badRequestResult)
            {
                return BadRequest(badRequestResult.Value);
            }
            else
            {
                return StatusCode(500, "Unexpected result type");
            }
        }

        [Authorize(Roles = "Admin,SuperAdmin")]
        [HttpPut("update/{id}")]
        public async Task<ActionResult<ArticleUpdateDto>> UpdateArticle(Guid id, [FromBody] ArticleUpdateDto updateArticle)
        {
            var updatedArticle = await _articleService.UpdateArticle(id, updateArticle);
            return Ok(updatedArticle);
        }

        [HttpGet("for-you"), Authorize(Roles = "User,Premium,Admin,SuperAdmin")]
        public async Task<IActionResult> GetForYouArticles()
        {
            var forYouArticles = await _articleService.GetForYouArticles();
            var forYouArticlesList = (OkObjectResult) forYouArticles;
            return Ok(forYouArticlesList.Value);
        }

        [AllowAnonymous]
        [HttpGet("recommended")]
        public async Task<IActionResult> GetRecommendetArticles()
        {
            var recommendedArticles = await _articleService.GetRecommendedArticles();
            var recommendedArticlesList = (OkObjectResult) recommendedArticles;
            return Ok(recommendedArticlesList);
        }

        [AllowAnonymous]
        [HttpGet("trending")]
        public async Task<IActionResult> GetTrendingArticles()
        {
            var trendingArticles = await _articleService.GetTrendingArticles();
            var trendingArticlesList = (OkObjectResult) trendingArticles;
            return Ok(trendingArticlesList.Value);
        }

        [AllowAnonymous]
        [HttpGet("category")]
        public async Task<IActionResult> GetArticlesByCategory(int categoryId, string? categoryName, string? range = null)
        {
            var articles = await _articleService.GetArticlesByCategory(categoryId, categoryName);

            if (articles is OkObjectResult okResult)
            {
                return Ok(okResult.Value);
            }
            else if (articles is NotFoundResult)
            {
                return NotFound();
            }
            else if (articles is BadRequestObjectResult badRequestResult)
            {
                return BadRequest(badRequestResult.Value);
            }
            else
            {
                return StatusCode(500, "Unexpected result type");
            }
        }

        [AllowAnonymous]
        [HttpGet("tag")]
        public async Task<IActionResult> GetArticlesByTag(string? tagName, string? range = null)
        {
            var articles = await _articleService.GetArticlesByTag(tagName, range);
            if (articles is OkObjectResult okResult)
            {
                return Ok(okResult.Value);
            }
            else if (articles is NotFoundResult)
            {
                return NotFound();
            }
            else if (articles is BadRequestObjectResult badRequestResult)
            {
                return BadRequest(badRequestResult.Value);
            }
            else
            {
                return StatusCode(500, "Unexpected result type");
            }
        }

        [AllowAnonymous]
        [HttpGet("source")]
        public async Task<IActionResult> GetArticlesBySource(Guid sourceId, string? sourceName, string? range = null)
        {
            var articles = await _articleService.GetArticlesBySource(sourceId, sourceName,range);
            if (articles is OkObjectResult okResult)
            {
                return Ok(okResult.Value);
            }
            else if (articles is NotFoundResult)
            {
                return NotFound();
            }
            else if (articles is BadRequestObjectResult badRequestResult)
            {
                return BadRequest(badRequestResult.Value);
            }
            else
            {
                return StatusCode(500, "Unexpected result type");
            }
        }

        [AllowAnonymous]
        [HttpPost("like/{articleId}")]
        public async Task<IActionResult> LikeArticle(Guid articleId)
        {
            var result = await _articleService.LikeArticle(articleId);
            return Ok(result);
        }

        [AllowAnonymous]
        [HttpPost("view/{articleId}")]
        public async Task<IActionResult> AddView(Guid articleId)
        {
            var result = await _articleService.AddView(articleId);
            return Ok(result);
        }

        [AllowAnonymous]
        [HttpPost("search")]
        public async Task<IActionResult> SearchArticlesAsync(string query, string? range = null)
        {
            var articles = await _articleService.SearchArticlesAsync(query, range);
            
            return Ok(articles);
        }

    }
}


