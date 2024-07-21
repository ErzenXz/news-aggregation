using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NewsAggregation.DTO.Article;

namespace NewsAggregation.Services.Interfaces
{
    public interface IArticleService
    {
        Task<IActionResult> CreateArticle(ArticleCreateDto article);
        Task<IActionResult> DeleteArticle(Guid id);
        Task<IActionResult> GetArticleById(Guid id);

        Task<IActionResult> GetAllArticles();
        Task<IActionResult> UpdateArticle(Guid id, ArticleUpdateDto updateArticle);

    }
}
