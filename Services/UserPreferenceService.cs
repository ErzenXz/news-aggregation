using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using News_aggregation.Entities;
using NewsAggregation.Data.UnitOfWork;
using NewsAggregation.DTO.UserPreferences;
using NewsAggregation.Models;
using NewsAggregation.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NewsAggregation.Services
{
    public class UserPreferenceService : IUserPreferenceService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<AuthService> _logger;

        public UserPreferenceService(IMapper mapper, IUnitOfWork unitOfWork, ILogger<AuthService> logger)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<IActionResult> CreateUserPreferences(UserPreferencesCreateDto createUserPreferencesDto)
        {
            try
            {
                var userPreferences = _mapper.Map<UserPreference>(createUserPreferencesDto);

                _unitOfWork.Repository<UserPreference>().Create(userPreferences);

                await _unitOfWork.CompleteAsync();

                var createdUserPreferencesDto = _mapper.Map<UserPreferencesCreateDto>(userPreferences);
                return new OkObjectResult(createdUserPreferencesDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Creating User Preferences");
                return new StatusCodeResult(500);
            }
        }

        public async Task<IActionResult> DeleteUserPreferences(Guid id)
        {
            try
            {
                var userPreferencesToDelete = await _unitOfWork.Repository<UserPreference>().GetById(id);

                if (userPreferencesToDelete != null)
                {
                    _unitOfWork.Repository<UserPreference>().Delete(userPreferencesToDelete);
                    await _unitOfWork.CompleteAsync();
                    return new OkResult();
                }
                else
                {
                    _logger.LogWarning($"User Preferences with id {id} not found.");
                    return new NotFoundResult();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Deleting User Preferences");
                return new StatusCodeResult(500);
            }
        }

        public async Task<IActionResult> GetUserPreferencesById(Guid id)
        {
            try
            {
                var userPreferences = await _unitOfWork.Repository<UserPreference>().GetById(id);

                if (userPreferences == null)
                {
                    _logger.LogWarning($"User Preferences with id {id} not found.");
                    return new NotFoundResult();
                }

                var userPreferencesDto = _mapper.Map<UserPreferencesCreateDto>(userPreferences);
                return new OkObjectResult(userPreferencesDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetUserPreferencesById");
                return new StatusCodeResult(500);
            }
        }

        public async Task<IActionResult> GetAllUserPreferences()
        {
            try
            {
                var userPreferences = await _unitOfWork.Repository<UserPreference>().GetAll().ToListAsync();

                var userPreferencesDto = _mapper.Map<List<UserPreferencesCreateDto>>(userPreferences);
                return new OkObjectResult(userPreferencesDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAllUserPreferences");
                return new StatusCodeResult(500);
            }
        }

        public async Task<IActionResult> UpdateUserPreferences(Guid id, UserPreferencesCreateDto updateUserPreferencesDto)
        {
            try
            {
                var userPreferences = await _unitOfWork.Repository<UserPreference>().GetById(id);
                if (userPreferences == null)
                {
                    _logger.LogWarning($"User Preferences with id {id} not found.");
                    return new NotFoundResult();
                }

                userPreferences.UserId = updateUserPreferencesDto.UserId;
                userPreferences.CategoryId = updateUserPreferencesDto.CategoryId;
                userPreferences.Tags = updateUserPreferencesDto.Tags;

                await _unitOfWork.CompleteAsync();

                var updatedUserPreferencesDto = _mapper.Map<UserPreferencesCreateDto>(userPreferences);
                return new OkObjectResult(updatedUserPreferencesDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateUserPreferences");
                return new StatusCodeResult(500);
            }
        }
    }
}
