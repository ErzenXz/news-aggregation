using Microsoft.AspNetCore.Mvc;
using NewsAggregation.DTO.Article;
using NewsAggregation.Services;
using NewsAggregation.Services.Interfaces;
using System;
using System.Threading.Tasks;

namespace NewsAggregation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ArticleController : ControllerBase
    {
        private readonly IArticleService _articleService;

        public ArticleController(IArticleService articleService)
        {
            _articleService = articleService;
        }

        [HttpPost("CreateArticle")]
        public async Task<ActionResult<ArticleCreateDto>> CreateArticle(ArticleCreateDto createArticle)
        {

            var createdArticle = await _articleService.CreateArticle(createArticle);
            return Ok(createdArticle);


        }

        [HttpDelete("DeleteArticle/{id}")]
        public async Task<IActionResult> DeleteArticle(Guid id)
        {

            await _articleService.DeleteArticle(id);
            return NoContent();


        }

        [HttpGet("GetArticleById/{id}")]
        public async Task<ActionResult<ArticleCreateDto>> GetArticleById(Guid id)
        {

            var article = await _articleService.GetArticleById(id);

            return Ok(article);
        }

        [HttpGet("GetAllArticles")]
        public async Task<ActionResult<List<ArticleCreateDto>>> GetAllArticles()
        {
            var articles = await _articleService.GetAllArticles();
            return Ok(articles);
        }

        [HttpPut("UpdateArticle/{id}")]
        public async Task<ActionResult<ArticleUpdateDto>> UpdateArticle(Guid id, [FromBody] ArticleUpdateDto updateArticle)
        {
            var updatedArticle = await _articleService.UpdateArticle(id, updateArticle);
            return Ok(updatedArticle);
        }

        [HttpGet("GetPagedArticles")]
        public async Task<IActionResult> PagedArticlesView(int page, int pageSize, string searchByTitle)
        {
            var articleList = await _articleService.PagedArticlesView( page,pageSize,searchByTitle);

            return Ok(articleList);
        }

    }
}


