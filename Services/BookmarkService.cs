using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using News_aggregation.Entities;
using NewsAggregation.Data;
using NewsAggregation.Data.UnitOfWork;
using NewsAggregation.DTO.Article;
using NewsAggregation.DTO.Favorite;
using NewsAggregation.Helpers;
using NewsAggregation.Models;
using NewsAggregation.Services.Interfaces;
using QRCoder;
using System;
using System.IdentityModel.Tokens.Jwt;

namespace NewsAggregation.Services;

public class BookmarkService : IBookmarkService
{
    private readonly DBContext _dBContext;
    private readonly ILogger<BookmarkService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public BookmarkService(DBContext dBContext, IHttpContextAccessor httpContextAccessor, ILogger<BookmarkService> logger)
    {
        _dBContext = dBContext;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IActionResult> GetAllBookmarks(string? range = null)
    {
        var queryParams = ParameterParser.ParseRangeAndSort(range, "sort");
        var page = queryParams.Page;
        var pageSize = queryParams.PerPage;

        try
        {
            var articleList = new List<ArticleDto>();

            var bookmarks = await _dBContext.Bookmarks.Take(pageSize).Skip((page - 1) * pageSize).ToListAsync();

            foreach (var bookmark in bookmarks)
            {
                var article = await _dBContext.Articles.FirstOrDefaultAsync(x => x.Id == bookmark.ArticleId);
                var articleDTO = new ArticleDto
                {
                    Title = article.Title,
                    Content = article.Content,
                    ImageUrl = article.ImageUrl,
                    PublishedAt = article.CreatedAt,
                    likes = article.Likes,
                    Tags = article.Tags,
                };

                articleList.Add(articleDTO);
            }

            return new OkObjectResult(articleList);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error in GetAllBookmarks");
            return new StatusCodeResult(500);
        }
    }

    public async Task<IActionResult> GetBookmarksByArticleId(Guid articleId, string? range = null)
    {
        try
        {
            var queryParams = ParameterParser.ParseRangeAndSort(range, "sort");
            var page = queryParams.Page;
            var pageSize = queryParams.PerPage;

            if (Guid.Empty == articleId)
                return new BadRequestObjectResult(new { Message = "Invalid article id.", Code = 75 });

            var bookmark = await _dBContext.Bookmarks.Where(x => x.ArticleId == articleId).Take(pageSize).Skip((page - 1) * pageSize)
                .ToListAsync();

            if (bookmark.Count == 0)
            {
                return new NotFoundObjectResult(new { Message = "No bookmarks found for the given article.", Code = 79 });
            }

            return new OkObjectResult(bookmark);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error in GetBookmarksByArticle");
            return new StatusCodeResult(500);
        }
    }
    public async Task<IActionResult> GetBookmarkById(Guid id)
    {
        try
        {
            if (id == Guid.Empty)
            {
                return new BadRequestObjectResult(new { Message = "Invalid bookmark id.", Code = 77 });
            }
            var bookmark = await _dBContext.Bookmarks.FirstOrDefaultAsync(x => x.Id == id);

            if (bookmark == null)
            {
                return new NotFoundObjectResult(new { Message = "Bookmark not found.", Code = 78 });
            }

            var article = await _dBContext.Articles.FirstOrDefaultAsync(x => x.Id == bookmark.ArticleId);
            return new OkObjectResult(article);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error in GetBookmarkById");
            return new StatusCodeResult(500);
        }
    }

    public async Task<IActionResult> CreateBookmark(BookmarkCreateDto bookmark)
    {
        try
        {
            var httpContex = _httpContextAccessor.HttpContext;

            var refreshToken = httpContex.Request.Cookies["refreshToken"];
            var userAgent = httpContex.Request.Headers["User-Agent"].ToString();

            if (refreshToken == null)
            {
                return new UnauthorizedObjectResult(new { Message = "Unauthorized to perform this request.", Code = 76 });
            }

            var user = await FindUserByRefreshToken(refreshToken, userAgent);

            if (user == null)
            {
                return new UnauthorizedObjectResult(new { Message = "Unauthorized to perform this request.", Code = 76 });
            }

            var bookmarkToAdd = new Bookmark
            {
                ArticleId = bookmark.ArticleId,
                UserId = user.Id
            };

            await _dBContext.Bookmarks.AddAsync(bookmarkToAdd);
            await _dBContext.SaveChangesAsync();

            return new OkResult();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error in CreateBookmark");
            return new StatusCodeResult(500);
        }
    }

    public async Task<IActionResult> DeleteBookmark(Guid id)
    {
        try
        {

            if (id == Guid.Empty)
            {
                return new BadRequestObjectResult(new { Message = "Invalid bookmark id.", Code = 77 });
            }

            var httpContex = _httpContextAccessor.HttpContext;

            var refreshToken = httpContex.Request.Cookies["refreshToken"];
            var userAgent = httpContex.Request.Headers["User-Agent"].ToString();

            if (refreshToken == null)
            {
                return new UnauthorizedObjectResult(new { Message = "Unauthorized to perform this request.", Code = 76 });
            }

            var user = await FindUserByRefreshToken(refreshToken, userAgent);

            if (user == null)
            {
                return new UnauthorizedObjectResult(new { Message = "Unauthorized to perform this request.", Code = 76 });
            }

            var bookmark = _dBContext.Bookmarks.FirstOrDefault(x => x.Id == id);

            if (bookmark == null)
            {
                return new NotFoundObjectResult(new { Message = "Bookmark not found.", Code = 78 });
            }

            var jwt = httpContex.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            var token = new JwtSecurityToken(jwt);
            var role = token.Claims.FirstOrDefault(c => c.Type == "role")?.Value;

            if (role.Equals("Admin") || role.Equals("SuperAdmin"))
            {
                _dBContext.Bookmarks.Remove(bookmark);
                await _dBContext.SaveChangesAsync();

                return new OkResult();
            }
            else
            {
                if (bookmark.UserId != user.Id)
                {
                    return new UnauthorizedObjectResult(new { Message = "Unauthorized to perform this request.", Code = 76 });
                } else
                {
                    _dBContext.Bookmarks.Remove(bookmark);
                    await _dBContext.SaveChangesAsync();
                }

            }

            return new OkResult();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error in CreateBookmark");
            return new StatusCodeResult(500);
        }

    }

    public async Task<User?> FindUserByRefreshToken(string refreshToken, string userAgent)
    {
        var currentTime = DateTime.UtcNow;

        var refreshTokenEntry = _dBContext.refreshTokens.FirstOrDefault(r => r.Token == refreshToken && r.Expires > currentTime && r.UserAgent == userAgent && r.Revoked == null);

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