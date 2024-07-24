﻿using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
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

        Task<PagedInfo<ArticleDto>> PagedArticlesView(int page, int pageSize, string searchByTitle);

        //Task<IActionResult> GetArticlesByCategory(Guid categoryId);
        //Task<IActionResult> GetArticlesByTag(Guid tagId);
        Task<IActionResult> GetRecommendetArticles();

    }
}