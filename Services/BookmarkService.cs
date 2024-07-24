using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using News_aggregation.Entities;
using NewsAggregation.Data.UnitOfWork;
using NewsAggregation.DTO.Favorite;
using NewsAggregation.Helpers;
using NewsAggregation.Services.Interfaces;
using QRCoder;

namespace NewsAggregation.Services;

public class BookmarkService : IBookmarkService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<AuthService> _logger;

    public BookmarkService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<AuthService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }
    
    public async Task<IActionResult> GetAllBookmarks(string? range = null)
    {
        var queryParams = ParameterParser.ParseRangeAndSort(range, "sort");
        var page = queryParams.Page;
        var pageSize = queryParams.PerPage;
        
        try
        {
            var bookmarks = await _unitOfWork.Repository<Bookmark>().GetAll()
                .Include(x => x.Article)
                .Include(x => x.User)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .ToListAsync();

            return new ObjectResult(bookmarks);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error in GetAllBookmarks");
            return new StatusCodeResult(500);
        }
    }

    public async Task<IActionResult> GetBookmarksByArticleId(Guid articleId)
    {
        try
        {
            var bookmark = await _unitOfWork.Repository<Bookmark>().GetByCondition(x => x.ArticleId == articleId)
                .Include(x => x.Article)
                .Include(x => x.User)
                .ToListAsync();

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
            var bookmark = await _unitOfWork.Repository<Bookmark>().GetByCondition(x => x.Id == id)
                .Include(x => x.Article)
                .Include(x => x.User)
                .FirstOrDefaultAsync();
            
            return new OkObjectResult(bookmark);
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
            var bookmarkToCreate = _mapper.Map<Bookmark>(bookmark);
            //bookmarkToCreate.Id = Guid.NewGuid();
            _unitOfWork.Repository<Bookmark>().Create(bookmarkToCreate);
            _unitOfWork.Complete();

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
            var bookmarkToDelete =
                await _unitOfWork.Repository<Bookmark>().GetById(id);

            if (bookmarkToDelete == null)
            {
                _logger.LogWarning("Bookmark not found");
                return new NotFoundResult();
            }
        
            _unitOfWork.Repository<Bookmark>().Delete(bookmarkToDelete);
            _unitOfWork.Complete();

            return new OkResult();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error in CreateBookmark");
            return new StatusCodeResult(500);
        }
        
    }
}