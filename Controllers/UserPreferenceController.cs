using Microsoft.AspNetCore.Authorization;
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
    [Route("preferences")]
    public class UserPreferencesController : ControllerBase
    {
        private readonly IUserPreferenceService _userPreferenceService;

        public UserPreferencesController(IUserPreferenceService userPreferenceService)
        {
            _userPreferenceService = userPreferenceService;
        }

        [HttpPost("add")]
        public async Task<ActionResult<UserPreferencesCreateDto>> CreateUserPreferences(UserPreferencesCreateDto createUserPreferences)
        {
            var createdUserPreferences = await _userPreferenceService.CreateUserPreferences(createUserPreferences);
            return Ok(createdUserPreferences);
        }

        [HttpDelete("remove/{id}")]
        public async Task<IActionResult> DeleteUserPreferences(Guid id)
        {
            await _userPreferenceService.DeleteUserPreferences(id);
            return NoContent();
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserPreferencesById(Guid id)
        {
            var userPreferences = await _userPreferenceService.GetUserPreferencesById(id);
            return Ok(userPreferences);
        }


        [HttpGet("all")]
        public async Task<IActionResult> GetAllPreferences(string? range = null)
        {
            var userPreferences = await _userPreferenceService.GetAllPreferences(range);
            return Ok(userPreferences);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUserPreferences(Guid id, [FromBody] UserPreferencesCreateDto updateUserPreferences)
        {
            var updatedUserPreferences = await _userPreferenceService.UpdateUserPreferences(id, updateUserPreferences);
            return Ok(updatedUserPreferences);
        }
    }
}
