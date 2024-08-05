using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using News_aggregation.Entities;
using NewsAggregation.DTO.Article;
using NewsAggregation.Helpers;

namespace NewsAggregation.Services.Interfaces
{
    public interface IArticleService
    {
        Task<IActionResult> CreateArticle(ArticleCreateDto article);
        Task<IActionResult> DeleteArticle(Guid id);
        Task<IActionResult> GetArticleById(Guid id);

        Task<IActionResult> GetAllArticles(string? range = null);
        Task<IActionResult> UpdateArticle(Guid id, ArticleUpdateDto updateArticle);

        Task<IActionResult> GetArticlesByCategory(int categoryId, string? categoryName, string? range = null);
        Task<IActionResult> GetArticlesByTag(string? tagName, string? range = null);
        Task<IActionResult> GetArticlesBySource(Guid sourceId, string? sourceName, string? range = null);

        Task<IActionResult> GetRecommendedArticles();
        Task<IActionResult> GetTrendingArticles();
        Task<IActionResult> GetForYouArticles();

        Task<IActionResult> LikeArticle(Guid articleId);
        Task<IActionResult> AddView(Guid articleId);
        Task<IEnumerable<Article>> SearchArticlesAsync(string query, string? range = null);

    }
}
