using Microsoft.AspNetCore.Mvc;
using NewsAggregation.DTO.UserPreferences;
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
        Task<IActionResult> GetAllUserPreferences();
        Task<IActionResult> UpdateUserPreferences(Guid id, UserPreferencesCreateDto updateUserPreferencesDto);


    }
}
