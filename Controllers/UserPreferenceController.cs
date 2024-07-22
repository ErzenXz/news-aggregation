using Microsoft.AspNetCore.Mvc;
using NewsAggregation.DTO.UserPreferences;
using NewsAggregation.Services;
using NewsAggregation.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NewsAggregation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserPreferencesController : ControllerBase
    {
        private readonly IUserPreferenceService _userPreferenceService;

        public UserPreferencesController(IUserPreferenceService userPreferenceService)
        {
            _userPreferenceService = userPreferenceService;
        }

        [HttpPost("CreateUserPreferences")]
        public async Task<ActionResult<UserPreferencesCreateDto>> CreateUserPreferences(UserPreferencesCreateDto createUserPreferences)
        {
            var createdUserPreferences = await _userPreferenceService.CreateUserPreferences(createUserPreferences);
            return Ok(createdUserPreferences);
        }

        [HttpDelete("DeleteUserPreferences/{id}")]
        public async Task<IActionResult> DeleteUserPreferences(Guid id)
        {
            await _userPreferenceService.DeleteUserPreferences(id);
            return NoContent();
        }

        [HttpGet("GetUserPreferencesById/{id}")]
        public async Task<ActionResult<UserPreferencesCreateDto>> GetUserPreferencesById(Guid id)
        {
            var userPreferences = await _userPreferenceService.GetUserPreferencesById(id);
            return Ok(userPreferences);
        }

        [HttpGet("GetAllUserPreferences")]
        public async Task<ActionResult<List<UserPreferencesCreateDto>>> GetAllUserPreferences()
        {
            var userPreferences = await _userPreferenceService.GetAllUserPreferences();
            return Ok(userPreferences);
        }

        [HttpPut("UpdateUserPreferences/{id}")]
        public async Task<ActionResult<UserPreferencesCreateDto>> UpdateUserPreferences(Guid id, [FromBody] UserPreferencesCreateDto updateUserPreferences)
        {
            var updatedUserPreferences = await _userPreferenceService.UpdateUserPreferences(id, updateUserPreferences);
            return Ok(updatedUserPreferences);
        }
    }
}
