using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using NewsAggregation.Data;
using NewsAggregation.DTO;
using NewsAggregation.Models;
using NewsAggregation.Services;
using NewsAggregation.Services.Interfaces;
using System.Security.Cryptography;

namespace NewsAggregation.Controllers
{
    [ApiController]
    [Route("user")]
    public class UserController : ControllerBase
    {


        private readonly ILogger<UserController> _logger;
        private readonly DBContext _dBContext;
        private readonly IUserService _userService;
        private readonly IUserPreferenceService _userPreferenceService;

        public UserController(ILogger<UserController> logger, DBContext dBContext, IUserService userService, IUserPreferenceService userPreferenceService)
        {
            _logger = logger;
            _dBContext = dBContext;
            _userService = userService;
            _userPreferenceService = userPreferenceService;
        }


        
         /*
        [HttpGet("{id}"), Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<User> GetUserById(long id)
        {
            var user = await _dBContext.Users.FindAsync(id);
            if (user == null)
            {
                _logger.LogInformation("User request for " + id + " has failed!");

                return null;
            }

            _logger.LogInformation("User request for " + id + " has been successful!");

            // Add Content-Range header
            Response.Headers.Add("Content-Range", $"users 0-0/1");

            return user;
            
        }
        
        [HttpGet("username/{username}"), Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<User?> GetUserByUsername(string username)
        {
            var user = await _dBContext.Users.FirstOrDefaultAsync(u => u.Username == username);

            if (user == null)
            {
                _logger.LogInformation("User request for " + username + " has failed!");

                return null;
            }

            _logger.LogInformation("User request for " + username + " has been successful!");

            // Add Content-Range header
            Response.Headers.Add("Content-Range", $"users 0-0/1");


            return user;
        }

        [HttpGet("email/{email}"), Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<User> GetUsersByEmail(string email)
        {
            var user = await _dBContext.Users.FirstOrDefaultAsync(u => u.Email == email);

            if(user == null)
            {
               return null;
            }

            // Add Content-Range header
            Response.Headers.Add("Content-Range", $"users 0-0/1");
 
            return user;

        }

        [HttpGet("fullname/{fullname}"), Authorize(Roles = "Admin,SuperAdmin")]
        public async IAsyncEnumerable<User> GetUsersByFullName(string fullname)
        {
            var users = _dBContext.Users.Where(u => u.Username == fullname).AsAsyncEnumerable();
            await foreach (var user in users)
            {
                yield return user;
            }

        }

        [HttpGet, Authorize(Roles = "Admin,SuperAdmin")]
        public async IAsyncEnumerable<User> GetAllUsers(string? range = null)
        {
            // Parse the range query parameter

            var queryParams = ParameterParser.ParseRangeAndSort(range, "sort");

            var users = _dBContext.Users.Skip((queryParams.Page - 1) * queryParams.PerPage).Take(queryParams.PerPage).AsAsyncEnumerable();

            // Add Content-Range header
            Response.Headers.Add("Content-Range", $"users {queryParams.Page * queryParams.PerPage}-{(queryParams.Page * queryParams.PerPage) + queryParams.PerPage}/{_dBContext.Users.Count()}");


            await foreach (var user in users)
            {
                yield return user;
            }
        }
    */

        [HttpGet("active-sessions"),
            Authorize(Roles = "User,Admin,SuperAdmin")]
        public async Task<IActionResult> GetActiveSessions()
        {
           var sessions = await _userService.GetActiveSessions();
            return sessions;
        }

        [HttpDelete("revoke-session"),
                       Authorize(Roles = "User,Admin,SuperAdmin")]
        public async Task<IActionResult> RevokeActiveSession(string? ipAdress, string userAgent)
        {
            var response = await _userService.RevokeActiveSession(ipAdress, userAgent);
            return response;
        }

        [HttpPut("{id}"),Authorize(Roles ="User,Admin,SuperAdmin")]
        public async Task<IActionResult> UpdateUser(UserUpdateRequest userUpdateRequest)
        {
            var response = await _userService.UpdateUser(userUpdateRequest);
            return response;
        }    

        [HttpPatch("update/birthdate"), Authorize(Roles = "User,Admin,SuperAdmin")]
        public async Task<IActionResult> UpdateUserBirthdate(DateTime newBirthDay)
        {
            var response = await _userService.UpdateUserBirthdate(newBirthDay);
            return response;
        }

        [HttpPatch("update/username"), Authorize(Roles = "User,Admin,SuperAdmin")]
        public async Task<IActionResult> UpdateUserUsername(string newUsername)
        {
            var response = await _userService.UpdateUserUsername(newUsername);
            return response;

        }

        [HttpPatch("update/fullname"), Authorize(Roles = "User,Admin,SuperAdmin")]
        public async Task<IActionResult> UpdateUserFullName(string newName)
        {
            var response = await _userService.UpdateUserFullName(newName);
            return response;
        }

        [HttpPatch("update/profile-image"), Authorize(Roles = "User,Admin,SuperAdmin")]
        public async Task<IActionResult> UpdateProfileImage(string imageUrl)
        {
            var response = await _userService.UpdateProfileImage(imageUrl);
            return response;
        }

        [HttpGet("view-history"), Authorize(Roles = "User,Admin,SuperAdmin")]
        public async Task<IActionResult> GetViewHistory(string? range = null)
        {
            var response = await _userService.GetViewHistory(range);
            return response;
        }

        [HttpGet("saved-articles"), Authorize(Roles = "User,Admin,SuperAdmin")]
        public async Task<IActionResult> GetSavedArticles(string? range = null)
        {
            var response = await _userService.GetSavedArticles(range);
            return response;
        }

        [HttpGet("view-preferences"), Authorize(Roles = "User,Admin,SuperAdmin")]
        public async Task<IActionResult> GetAllUserPreferences(string? range = null)
        {
            var userPreferences = await _userPreferenceService.GetAllUserPreferences(range);

            if (userPreferences == null)
            {
                return BadRequest(new { Message = "User preferences not found.", Code = 36 });
            }

            return Ok(userPreferences);
        }

        /*
        [HttpDelete("delete/id/{id}"), Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> DeleteUserById(long id)
        {
            var user = await _dBContext.Users.FindAsync(id);
            if (user == null)
            {
                return BadRequest(new { Message = "User not found.", Code = 36 });
            }
            _dBContext.Users.Remove(user);
            await _dBContext.SaveChangesAsync();
            return Ok(new { Message = "User deleted successfully.", Code = 400 });
        }

        [HttpDelete("delete/username/{username}"), Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> DeleteUserByUsername(string username)
        {
            var user = await _dBContext.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null)
            {
                return BadRequest(new { Message = "User not found.", Code = 36 });
            }
            _dBContext.Users.Remove(user);
            await _dBContext.SaveChangesAsync();

            // Add Content-Range header
            Response.Headers.Add("Content-Range", $"users 0-0/1");

            return Ok(new { Message = "User deleted successfully.", Code = 400 });
        }

        */

        // Find the user by giving a refresh token
        [NonAction]
        public User? FindUserByRefreshToken(string refreshToken, string userAgent)
        {
            var currentTime = DateTime.UtcNow;

            var user = _dBContext.refreshTokens.FirstOrDefault(r => r.Token == refreshToken && r.Expires > currentTime && r.UserAgent == userAgent && r.Revoked == null);

            if (user == null)
            {
                return null;
            }

            return _dBContext.Users.FirstOrDefault(u => u.Id == user.UserId);

        }

    }
}
