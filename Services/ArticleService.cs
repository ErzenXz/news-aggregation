using AutoMapper;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using News_aggregation.Entities;
using NewsAggregation.Data.UnitOfWork;
using NewsAggregation.DTO.Article;
using NewsAggregation.Services.Interfaces;
using System;
using System.Threading.Tasks;

namespace NewsAggregation.Services
{
    public class ArticleService : IArticleService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<AuthService> _logger;

        public ArticleService(IMapper mapper, IUnitOfWork unitOfWork, ILogger<AuthService> logger)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<IActionResult> CreateArticle(ArticleCreateDto article)
        {
            try
            {
                var createArticle = _mapper.Map<Article>(article);

                _unitOfWork.Repository<Article>().Create(createArticle);

                await _unitOfWork.CompleteAsync();

                var createdArticleDto = _mapper.Map<ArticleCreateDto>(createArticle);
                return new OkObjectResult(createdArticleDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Creating Article");
                return new StatusCodeResult(500);
               
            }
        }

        public async Task<IActionResult> DeleteArticle(Guid id)
        {
            try
            {
                var articleToDelete = await _unitOfWork.Repository<Article>().GetById(id);

                if (articleToDelete != null)
                {
                    _unitOfWork.Repository<Article>().Delete(articleToDelete);
                    await _unitOfWork.CompleteAsync();
                    return new OkResult();
                }
                else
                {
                    _logger.LogWarning($"Article with id {id} not found.");
                    return new NotFoundResult();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Deleting Article");
                return new StatusCodeResult(500);
            }
        }

        public async Task<IActionResult> GetArticleById(Guid id)
        {
            try
            {
                var article = await _unitOfWork.Repository<Article>().GetById(id);

                if (article == null)
                {
                    _logger.LogWarning($"Article with id {id} not found.");
                    return new NotFoundResult();
                }

                var articleDto = _mapper.Map<ArticleDto>(article);
                return new OkObjectResult(articleDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetArticleById");
                return new StatusCodeResult(500);
            }
        }

        public async Task<IActionResult> GetAllArticles()
        {
            try
            {
                var articles = await _unitOfWork.Repository<Article>().GetAll().ToListAsync();

                var articlesDto = _mapper.Map<List<ArticleDto>>(articles);
                return new OkObjectResult(articlesDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAllArticles");
                return new StatusCodeResult(500);
            }
        }

        public async Task<IActionResult> UpdateArticle(Guid id, ArticleUpdateDto updateArticle)
        {
            try
            {
                var article = await _unitOfWork.Repository<Article>().GetById(id);
                if (article == null)
                {
                    _logger.LogWarning($"Article with id {id} not found.");
                    return new NotFoundResult();
                }

              

                article.Title = updateArticle.Title;
                article.Content = updateArticle.Content;
                article.AuthorId = updateArticle.AuthorId;
                article.UpdatedAt = DateTime.UtcNow;
                article.ImageUrl = updateArticle.ImageUrl;
                article.SourceId = updateArticle.SourceId;
                article.CategoryId = updateArticle.CategoryId;
                

                await _unitOfWork.CompleteAsync();

                var updatedArticleDto = _mapper.Map<ArticleUpdateDto>(article);
                return new OkObjectResult(updatedArticleDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateArticle");
                return new StatusCodeResult(500);
            }
        }
    }
}
