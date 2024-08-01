using Microsoft.AspNetCore.Mvc;
using NewsAggregation.DTO.UserPreferences;
using NewsAggregation.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NewsAggregation.Services.Interfaces
{
    public interface IUserPreferenceService
    {
        Task<IActionResult> CreateUserPreferences(UserPreferencesCreateDto createUserPreferencesDto);
        Task<IActionResult> DeleteUserPreferences(Guid id);
        Task<IActionResult> GetUserPreferencesById(Guid id);
        Task<IActionResult> GetAllUserPreferences(string? range = null);
        Task<IActionResult> GetAllPreferences(string? range = null);
        Task<IActionResult> UpdateUserPreferences(Guid id, UserPreferencesCreateDto updateUserPreferencesDto);
        Task<User?> FindUserByRefreshToken(string refreshToken, string userAgent);
    }
}
