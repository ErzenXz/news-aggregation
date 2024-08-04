using AutoMapper;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using News_aggregation.Entities;
using NewsAggregation.Data;
using NewsAggregation.Data.UnitOfWork;
using NewsAggregation.DTO.Comment;
using NewsAggregation.Helpers;
using NewsAggregation.Services.Interfaces;
using static QRCoder.PayloadGenerator;
using System.IdentityModel.Tokens.Jwt;
using NewsAggregation.Models;
using NewsAggregation.Models.Security;
using Microsoft.EntityFrameworkCore.Update;

namespace NewsAggregation.Services;

public class CommentService : ICommentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<AuthService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CommentService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<AuthService> logger, IHttpContextAccessor httpContextAccessor)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IActionResult> GetCommentsByArticleId(Guid articleId, string? range = null)
    {
        try
        {
            var queryParams = ParameterParser.ParseRangeAndSort(range, "sort");
            var page = queryParams.Page;
            var pageSize = queryParams.PerPage;

            var comments = await _unitOfWork.Repository<Comment>().GetByCondition(x => x.ArticleId == articleId).Skip((page - 1) * pageSize).Take(pageSize)
                .ToListAsync();

            return new OkObjectResult(comments);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error in GetCommentsByArticle");
            return new BadRequestObjectResult(e.Message);
        }

    }

    public async Task<IActionResult> GetAllComments(string? range = null)
    {
        var queryParams = ParameterParser.ParseRangeAndSort(range, "sort");
        var page = queryParams.Page;
        var pageSize = queryParams.PerPage;

        try
        {
            var comments = await _unitOfWork.Repository<Comment>().GetAll()
                .Skip((page - 1) * pageSize).Take(pageSize)
                .ToListAsync();

            return new OkObjectResult(comments);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error in GetAllComments");
            return new BadRequestObjectResult(e.Message);
        }
    }

    public async Task<IActionResult> GetCommentById(Guid id)
    {
        try
        {
            var comment = await _unitOfWork.Repository<Comment>().GetByCondition(x => x.Id == id)
                .Include(x => x.Article)
                .Include(x => x.User)
                .FirstOrDefaultAsync();
            if (comment == null)
            {
                _logger.LogWarning("Comment not found");
                return new NotFoundResult();
            }

            return new OkObjectResult(comment);
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

            var commentToUpdate =
                await _unitOfWork.Repository<Comment>().GetById(id);

            if (commentToUpdate == null)
            {
                _logger.LogWarning("Comment with that ID is not found");
                return new NotFoundResult();
            }
            else
            {

                if (user.Id != commentToUpdate.UserId)
                    return new UnauthorizedObjectResult(new { Message = "Unauthorized to perform this request.", Code = 76 });

                commentToUpdate.Content = comment.Content;
                commentToUpdate.UpdatedAt = DateTime.UtcNow;
                

                await _unitOfWork.CompleteAsync();

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

            var httpContex = _httpContextAccessor.HttpContext;
            var refreshToken = httpContex.Request.Cookies["refreshToken"];
            var userAgent = httpContex.Request.Headers["User-Agent"].ToString();

            var jwt = httpContex.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            var token = new JwtSecurityToken(jwt);
            var role = token.Claims.FirstOrDefault(c => c.Type == "role")?.Value;

            if (refreshToken == null)
            {
                return new UnauthorizedObjectResult(new { Message = "Unauthorized to perform this request.", Code = 76 });
            }

            var user = await FindUserByRefreshToken(refreshToken, userAgent);

            if (user == null)
            {
                return new UnauthorizedObjectResult(new { Message = "Unauthorized to perform this request.", Code = 76 });
            }


            var commentToDelete =
                await _unitOfWork.Repository<Comment>().GetById(id);

            if (commentToDelete == null)
            {
                _logger.LogWarning("Comment not found");
                return new NotFoundResult();
            }
            else
            {

                if (user.Id != commentToDelete.UserId || user.Role.Equals("Admin") || user.Role.Equals("SuperAdmin"))
                    return new UnauthorizedObjectResult(new { Message = "Unauthorized to perform this request.", Code = 76 });

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

    public async Task<User?> FindUserByRefreshToken(string refreshToken, string userAgent)
    {
        var currentTime = DateTime.UtcNow;

        var refreshTokenEntry = await _unitOfWork.Repository<RefreshTokens>().GetByCondition(r => r.Token == refreshToken && r.Expires > currentTime && r.UserAgent == userAgent && r.Revoked == null).FirstOrDefaultAsync();

        if (refreshTokenEntry == null)
        {
            return null;
        }

        var userId = refreshTokenEntry.UserId;
        var refreshTokenVersion = refreshTokenEntry.TokenVersion;

        var user = await _unitOfWork.Repository<User>().GetByCondition(u => u.Id == userId).FirstOrDefaultAsync();

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

    public async Task<IActionResult> ReportComment(CommentReportDto commentReportDto)
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


            var comment = await _unitOfWork.Repository<Comment>().GetById(commentReportDto.CommentId);

            if (comment == null)
            {
                _logger.LogWarning("Comment not found");
                return new NotFoundResult();
            }

            if(comment.UserId == user.Id)
                return new UnauthorizedObjectResult(new { Message = "Unauthorized to perform this request.", Code = 76 });

            comment.IsReported = true;
            comment.ReportCount++;

            if (user != null)
            {
                var commentReportDTO = new CommentReports
                {
                    Id = Guid.NewGuid(),
                    CommentId = comment.Id,
                    UserId = user.Id,
                    ReportType = commentReportDto.ReportType,
                    CreatedAt = DateTime.UtcNow,
                    IpAddress = httpContex.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = userAgent,
                    Status = "Pending",
                    IsSeen = false
                };

                _unitOfWork.Repository<CommentReports>().Create(commentReportDTO);
            } else
            {
                var commentReportDTO = new CommentReports
                {
                    Id = Guid.NewGuid(),
                    CommentId = comment.Id,
                    UserId = null,
                    ReportType = commentReportDto.ReportType,
                    CreatedAt = DateTime.UtcNow,
                    IpAddress = httpContex.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = userAgent,
                    Status = "Pending",
                    IsSeen = false
                };

                _unitOfWork.Repository<CommentReports>().Create(commentReportDTO);
            }

            _unitOfWork.Repository<Comment>().Update(comment);

            await _unitOfWork.CompleteAsync();

            return new OkResult();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error in ReportComment");
            return new StatusCodeResult(500);
        }
    }

    public async Task<IActionResult> GetAllReportedComments(string? range = null)
    {
        try
        {
            var queryParams = ParameterParser.ParseRangeAndSort(range, "sort");
            var page = queryParams.Page;
            var pageSize = queryParams.PerPage;

            var reportedComments = await _unitOfWork.Repository<CommentReports>().GetByCondition(x => x.IsSeen == false)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .ToListAsync();

            return new OkObjectResult(reportedComments);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error in GetReportedComments");
            return new StatusCodeResult(500);
        }
    }

    public async Task<IActionResult> UpdateReportComment(Guid guid, sbyte status)
    {
        try
        {
            var commentReport = await _unitOfWork.Repository<CommentReports>().GetById(guid);

            if (commentReport == null)
            {
                _logger.LogWarning("Comment report not found");
                return new NotFoundResult();
            }

            commentReport.Status = status == 1 ? "Approved" : "Rejected";
            commentReport.IsSeen = true;

            _unitOfWork.Repository<CommentReports>().Update(commentReport);

            if (status == 1)
            {
                var comment = await _unitOfWork.Repository<Comment>().GetById(commentReport.CommentId);
                _unitOfWork.Repository<Comment>().Delete(comment);
            }

            await _unitOfWork.CompleteAsync();

            return new OkResult();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error in UpdateReportComment");
            return new StatusCodeResult(500);
        }
    }
 }