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
        public async Task<ActionResult<ArticleCreateDto>> CreateArticle(ArticleCreateDto createArticle)
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
            return Ok(articles);
        }

        [Authorize(Roles = "Admin,SuperAdmin")]
        [HttpPut("update/{id}")]
        public async Task<ActionResult<ArticleUpdateDto>> UpdateArticle(Guid id, [FromBody] ArticleUpdateDto updateArticle)
        {
            var updatedArticle = await _articleService.UpdateArticle(id, updateArticle);
            return Ok(updatedArticle);
        }

        [AllowAnonymous]
        [HttpGet("allPaged")]
        public async Task<IActionResult> PagedArticlesView(int page, int pageSize, string searchByTitle)
        {
            var articleList = await _articleService.PagedArticlesView( page,pageSize,searchByTitle);
            return Ok(articleList);
        }

        [AllowAnonymous]
        [HttpGet("recommended")]
        public async Task<IActionResult> GetRecommendetArticles()
        {
            var recommendedArticles = await _articleService.GetRecommendetArticles();
            return Ok(recommendedArticles);
        }

    }
}


