using AutoMapper;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using News_aggregation.Entities;
using NewsAggregation.Data.UnitOfWork;
using NewsAggregation.DTO.Comment;
using NewsAggregation.Helpers;
using NewsAggregation.Services.Interfaces;

namespace NewsAggregation.Services;

public class CommentService : ICommentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<AuthService> _logger;

    public CommentService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<AuthService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IActionResult> GetCommentsByArticleId(Guid articleId)
    {
        try
        {
            var comments = await _unitOfWork.Repository<Comment>().GetByCondition(x => x.ArticleId == articleId).ToListAsync();

            var commentsList = _mapper.Map<CommentDto>(comments);
            return new OkObjectResult(commentsList);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error in GetCommentsByArticle");
            return new StatusCodeResult(500);
        }
        
    }

    public async Task<IActionResult> GetAllComments(string? range = null)
    {
        var queryParams = ParameterParser.ParseRangeAndSort(range, "sort");
        var page = queryParams.Page;
        var pageSize = queryParams.PerPage;
        
        try
        {
            var comments = await _unitOfWork.Repository<Comment>().GetAll().Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            var commentsList = _mapper.Map<CommentDto>(comments);

            return new OkObjectResult(commentsList);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error in GetAllComments");
            return new StatusCodeResult(500);
        }
    }
    
    public async Task<IActionResult> GetCommentById(Guid id)
    {
        try
        {
            var comment = await _unitOfWork.Repository<Comment>().GetByCondition(x => x.Id == id).FirstOrDefaultAsync();
            if (comment == null)
            {
                _logger.LogWarning("Comment not found");
                return new NotFoundResult();
            }

            var commentToReturn = _mapper.Map<CommentDto>(comment);
            return new OkObjectResult(commentToReturn);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error in GetCommentById");
            return new StatusCodeResult(500);
        }
    }

    public async Task<IActionResult> CreateComment(CommentCreateDto comment)
    {
        try
        {
            var commentToCreate = _mapper.Map<Comment>(comment);
            
            //commentToCreate.Id = Guid.NewGuid();
            
            _unitOfWork.Repository<Comment>().Create(commentToCreate);
            await _unitOfWork.CompleteAsync();

            var createdComment = _mapper.Map<CommentDto>(commentToCreate);
            
            return new OkObjectResult(createdComment);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error in CreateComment");
            return new StatusCodeResult(500);
        }
        
    }

    public async Task<IActionResult> UpdateComment(Guid id, CommentDto comment)
    {
        try
        {
            var commentToUpdate =
                await _unitOfWork.Repository<Comment>().GetById(id);

            if (commentToUpdate == null)
            {
                _logger.LogWarning("Comment with that ID is not found");
                return new NotFoundResult();
            }
            else
            {
                commentToUpdate.Content = comment.Content;
                
                _unitOfWork.Repository<Comment>().Update(commentToUpdate);
                await _unitOfWork.CompleteAsync();

                var updatedComment = _mapper.Map<CommentDto>(commentToUpdate);

                return new OkResult();
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error in UpdateComment");
            return new StatusCodeResult(500);
        }
        

    }

    public async Task<IActionResult> DeleteComment(Guid id)
    {
        try
        {
            var commentToDelete =
                await _unitOfWork.Repository<Comment>().GetById(id);

            if (commentToDelete == null)
            {
                _logger.LogWarning("Comment not found");
                return new NotFoundResult();
            }
            else
            {
                _unitOfWork.Repository<Comment>().Delete(commentToDelete);
                await _unitOfWork.CompleteAsync();

                return new ObjectResult(commentToDelete.Id);
            }
            
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error in DeleteComment");
            return new StatusCodeResult(500);
        }
    }
}