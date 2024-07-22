using AutoMapper;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using News_aggregation.Entities;
using NewsAggregation.Data.UnitOfWork;
using NewsAggregation.DTO.Article;
using NewsAggregation.Helpers;
using NewsAggregation.Services.Interfaces;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using NewsAggregation.Models;
using NewsAggregation.Data;

namespace NewsAggregation.Services
{
    public class ArticleService : IArticleService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<AuthService> _logger;
        private readonly DBContext _dBContext;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ArticleService(IMapper mapper, IUnitOfWork unitOfWork, ILogger<AuthService> logger, DBContext dbContext, IHttpContextAccessor httpContextAccessor)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _dBContext = dbContext;
            _httpContextAccessor = httpContextAccessor;
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

        public async Task<IActionResult> GetAllArticles(string? range = null)
        {
            var queryParams = ParameterParser.ParseRangeAndSort(range, "sort");
            var page = queryParams.Page;
            var pageSize = queryParams.PerPage;

            

            try
            {
                var articles = await _unitOfWork.Repository<Article>().GetAll().Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

                // Get 3-5 random ads
                var ads = await _unitOfWork.Repository<Ads>().GetAll().OrderBy(a => Guid.NewGuid()).Take(new Random().Next(3, 5)).ToListAsync();


                // Add Content-Range header
                
                _httpContextAccessor.HttpContext.Response.Headers.Add("Content-Range", $"articles {page * pageSize}-{(page + 1) * pageSize - 1}/{articles.Count}");

                return new OkObjectResult(new {Articles = articles, Ads = ads});

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

        public async Task<PagedInfo<ArticleDto>> PagedArticlesView(int page, int pageSize, string searchByTitle)
        {
            try
            {
                var articles = _unitOfWork.Repository<Article>().GetAll();

                articles = articles.WhereIf(!string.IsNullOrEmpty(searchByTitle), a => a.Title.Contains(searchByTitle));

                var totalCount = await articles.CountAsync();
                var pagedArticles = await articles.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

                var mappedArticles = _mapper.Map<List<ArticleDto>>(pagedArticles);

                return new PagedInfo<ArticleDto>
                {
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    Items = mappedArticles
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetPagedArticles");
                throw;
            }
        }

        public async Task<IActionResult> GetRecommendetArticles()
        {
            try
            {
                // Get refresh token from cookies
                var refreshToken = _httpContextAccessor.HttpContext.Request.Cookies["refreshToken"];

                // Get user agent from request headers
                var userAgent = _httpContextAccessor.HttpContext.Request.Headers["User-Agent"].ToString();

                if (refreshToken == null)
                {
                    var articlesF = await _unitOfWork.Repository<Article>().GetAll().Take(5).ToListAsync();

                    return new OkObjectResult(articlesF);
                    
                }

                var user = await FindUserByRefreshToken(refreshToken, userAgent);

                if (user == null)
                {
                    var articlesF = await _unitOfWork.Repository<Article>().GetAll().Take(5).ToListAsync();

                    return new OkObjectResult(articlesF);
                }

                var userHistory = await _unitOfWork.Repository<UserHistory>().GetByCondition(uh => uh.UserId == user.Id).ToListAsync();

                // Get most used tags 
                var tags = userHistory.SelectMany(uh => uh.Tags.Split(",")).GroupBy(t => t).OrderByDescending(t => t.Count()).Select(t => t.Key).Take(5).ToList();

                var articles = await _unitOfWork.Repository<Article>().GetAll().Where(a => tags.Any(t => a.Tags.Contains(t))).Take(5).ToListAsync();

                // Content-Range header
                _httpContextAccessor.HttpContext.Response.Headers.Add("Content-Range", $"articles 0-4/{articles.Count}");

                return new OkObjectResult(articles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetRecommendetArticles");
                return new BadRequestObjectResult(new { Messages = ex.Message });
            }
        }


        public async Task<User?> FindUserByRefreshToken(string refreshToken, string userAgent)
        {
            var currentTime = DateTime.UtcNow;

            var refreshTokenEntry = _dBContext.refreshTokens.FirstOrDefault(r => r.Token == refreshToken && r.Expires > currentTime && r.UserAgent == userAgent);

            if (refreshTokenEntry == null)
            {
                return null;
            }

            var userId = refreshTokenEntry.UserId;
            var refreshTokenVersion = refreshTokenEntry.TokenVersion;

            var user = await _dBContext.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return null;
            }

            if (user.TokenVersion != refreshTokenVersion)
            {
                return null;
            }

            return user;
        }

    }
}
