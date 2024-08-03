using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using News_aggregation.Entities;
using NewsAggregation.Data;
using NewsAggregation.Data.UnitOfWork;
using NewsAggregation.DTO.UserPreferences;
using NewsAggregation.Models;
using NewsAggregation.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;
using static QRCoder.PayloadGenerator;

namespace NewsAggregation.Services
{
    public class UserPreferenceService : IUserPreferenceService
    {
        private readonly DBContext _dBContext;
        private readonly ILogger<AuthService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserPreferenceService(DBContext dBContext, ILogger<AuthService> logger, IHttpContextAccessor httpContextAccessor)
        {
            _dBContext = dBContext;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<IActionResult> CreateUserPreferences(UserPreferencesCreateDto createUserPreferencesDto)
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


                var userPreferences = new UserPreference
                {
                    UserId = user.Id,
                    CategoryId = createUserPreferencesDto.CategoryId
                };

                // Check if user already has a preference with the same category
                var existingUserPreference = await _dBContext.UserPreferences.FirstOrDefaultAsync(u => u.UserId == user.Id && u.CategoryId == createUserPreferencesDto.CategoryId);

                if (existingUserPreference != null)
                {
                    return new BadRequestObjectResult(new { Message = "User already has a preference with the same category", Code = 1000 });
                }

                await _dBContext.UserPreferences.AddAsync(userPreferences);
                await _dBContext.SaveChangesAsync();
                return new OkObjectResult(new { Message = "User Preferences Created Successfully", Code = 1000 });

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Creating User Preferences");
                return new StatusCodeResult(500);
            }
        }

        public async Task<IActionResult> DeleteUserPreferences(Guid id)
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

            try
            {
                var userPreferences = await _dBContext.UserPreferences.FirstOrDefaultAsync(u => u.Id == id);
                if (userPreferences == null)
                {
                    _logger.LogWarning($"User Preferences with id {id} not found.");
                    return new NotFoundResult();
                }


                var jwt = httpContex.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                var token = new JwtSecurityToken(jwt);
                var role = token.Claims.FirstOrDefault(c => c.Type == "role")?.Value;

                if (role.Equals("Admin") || role.Equals("SuperAdmin"))
                {
                    _dBContext.UserPreferences.Remove(userPreferences);
                    await _dBContext.SaveChangesAsync();

                    return new OkResult();
                } else
                {
                    if (userPreferences.UserId != user.Id)
                    {
                        return new UnauthorizedObjectResult(new { Message = "Unauthorized to perform this request.", Code = 76 });
                    }

                    _dBContext.UserPreferences.Remove(userPreferences);
                    await _dBContext.SaveChangesAsync();
                }

                return new OkObjectResult(new { Message = "User Preferences Deleted Successfully", Code = 200 });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteUserPreferences");
                return new StatusCodeResult(500);
            }

        }

        public async Task<IActionResult> GetUserPreferencesById(Guid id)
        {
            try
            {
                var userPreferences = await _dBContext.UserPreferences.FirstOrDefaultAsync(u => u.Id == id);
                if (userPreferences == null)
                {
                    _logger.LogWarning($"User Preferences with id {id} not found.");
                    return new NotFoundResult();
                }

                return new OkObjectResult(userPreferences);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetUserPreferencesById");
                return new StatusCodeResult(500);
            }
        }

        [HttpGet("mine"), Authorize(Roles = "User,Admin,SuperAdmin")]
        public async Task<IActionResult> GetAllUserPreferences(string? range = null)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var refreshToken = httpContext?.Request.Cookies["refreshToken"];
            var userAgent = httpContext?.Request.Headers.UserAgent.ToString();

            if (refreshToken == null)
            {
                return new UnauthorizedObjectResult(new { Message = "Unauthorized to perform this request.", Code = 76 });
            }

            var user = await FindUserByRefreshToken(refreshToken, userAgent);

            if (user == null)
            {
                return new UnauthorizedObjectResult(new { Message = "Unauthorized to perform this request.", Code = 76 });
            }

            try
            {
                var queryParams = ParameterParser.ParseRangeAndSort(range, "sort");
                var page = queryParams.Page;
                var pageSize = queryParams.PerPage;

                var userPreferences = await _dBContext.UserPreferences
                    .Where(u => u.UserId == user.Id)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var userPreferenceList = new List<UserPreferencesCreateDto>();
                foreach ( var userP in userPreferences)
                {
                    userPreferenceList.Add(new UserPreferencesCreateDto
                    {
                        CategoryId = userP.CategoryId,
                    });
                }

                return new OkObjectResult(userPreferenceList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAllUserPreferences");
                return new BadRequestObjectResult(new { Message = "An error occurred while processing your request.", Code = 500 });
            }
        }


        public async Task<IActionResult> GetAllPreferences(string? range = null)
        {
            var queryParams = ParameterParser.ParseRangeAndSort(range, "sort");
            var page = queryParams.Page;
            var pageSize = queryParams.PerPage;

            try
            {
                var userPreferences = await _dBContext.UserPreferences.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
                return new OkObjectResult(userPreferences);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAllPreferences");
                return new StatusCodeResult(500);
            }
        }

        public async Task<IActionResult> UpdateUserPreferences(Guid id, UserPreferencesCreateDto updateUserPreferencesDto)  
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

            try
            {
                var userPreferences = await _dBContext.UserPreferences.FirstOrDefaultAsync(u => u.Id == id);
                if (userPreferences == null)
                {
                    _logger.LogWarning($"User Preferences with id {id} not found.");
                    return new NotFoundResult();
                }

                userPreferences.CategoryId = updateUserPreferencesDto.CategoryId;

                _dBContext.UserPreferences.Update(userPreferences);
                await _dBContext.SaveChangesAsync();

                return new OkObjectResult(new { Message = "User Preferences Updated Successfully", Code = 200 });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateUserPreferences");
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
}
