using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using NewsAggregation.Data;
using NewsAggregation.DTO;
using NewsAggregation.Models;
using NewsAggregation.Services;
using System.Security.Cryptography;

namespace NewsAggregation.Controllers
{
    [ApiController]
    [Route("users")]
    public class UserController : ControllerBase
    {


        private readonly ILogger<UserController> _logger;
        private readonly DBContext _dBContext;

        public UserController(ILogger<UserController> logger, DBContext dBContext)
        {
            _logger = logger;
            _dBContext = dBContext;
        }


        
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


        [HttpPut("{id}"),Authorize(Roles ="User,Admin,SuperAdmin")]
        public async Task<IActionResult> UpdateUser(UserUpdateRequest userUpdateRequest)
        {

            var refreshToken = Request.Cookies["refreshToken"];

            if (refreshToken == null)
            {
                return Unauthorized(new { Message = "Unauthorized to perform this request.", Code = 76 });
            }

            var user = FindUserByRefreshToken(refreshToken, Request.Headers[HeaderNames.UserAgent].ToString());
            if (user == null)
            {
                return Unauthorized(new { Message = "Unauthorized to perform this request.", Code = 76 });
            }

            if (_dBContext.Users.Any(u => u.Username == userUpdateRequest.Username))
            {
                return BadRequest(new { Message = "Username already in use.", Code = 8 });
            }

            if (_dBContext.Users.Any(u => u.Email == userUpdateRequest.Email))
            {
                return BadRequest(new { Message = "Email already in use.", Code = 7 });
            }

            user.Username = userUpdateRequest.Username;
            user.FullName = userUpdateRequest.Fullname;
            user.Email = userUpdateRequest.Email;
            user.FirstLogin = userUpdateRequest.FirstLogin;
            user.LastLogin = userUpdateRequest.LastLogin;
            user.ConnectingIp = userUpdateRequest.ConIP;
            user.Birthdate = userUpdateRequest.Birthday;
            user.TimeZone = userUpdateRequest.TimeZone;
            user.Language = userUpdateRequest.Language;

            await _dBContext.SaveChangesAsync();

            // Add Content-Range header
            Response.Headers.Add("Content-Range", $"users 0-0/1");

            return Ok(new { Message = "User updated successfully.", Code = 79 });
        }    

        [HttpPatch("update/last-login"), Authorize(Roles = "User,Admin,SuperAdmin")]
        public async Task<IActionResult> UpdateUserLastLogin()
        {
            var refreshToken = Request.Cookies["refreshToken"];

            if (refreshToken == null)
            {
                return Unauthorized(new { Message = "Unauthorized to perform this request.", Code = 76 });
            }

            var user1 = FindUserByRefreshToken(refreshToken, Request.Headers[HeaderNames.UserAgent].ToString());


            if (user1 == null)
            {
                return Unauthorized(new { Message = "Unauthorized to perform this request.", Code = 76 });
            }

            user1.LastLogin = DateTime.UtcNow;
            await _dBContext.SaveChangesAsync();

            // Add Content-Range header
            Response.Headers.Add("Content-Range", $"users 0-0/1");

            return Ok(new { Message = "Last login updated successfully.", Code = 80 });
        }

        [HttpPatch("update/connecting-ip"), Authorize(Roles = "User,Admin,SuperAdmin")]
        public async Task<IActionResult> UpdateUserConnectingIp()
        {
            var refreshToken = Request.Cookies["refreshToken"];

            if (refreshToken == null)
            {
                return Unauthorized(new { Message = "Unauthorized to perform this request.", Code = 76 });
            }

            var user1 = FindUserByRefreshToken(refreshToken, Request.Headers[HeaderNames.UserAgent].ToString());

            if (user1 == null)
            {
                return Unauthorized(new { Message = "Unauthorized to perform this request.", Code = 76 });
            }


            user1.ConnectingIp = HttpContext.Connection.RemoteIpAddress.ToString();
            await _dBContext.SaveChangesAsync();

            // Add Content-Range header
            Response.Headers.Add("Content-Range", $"users 0-0/1");

            return Ok(new { Message = "Connecting IP updated successfully.", Code = 81 });
        }

        [HttpPatch("update/birthdate"), Authorize(Roles = "User,Admin,SuperAdmin")]
        public async Task<IActionResult> UpdateUserBirthdate(DateTime newBirthDay)
        {
            var refreshToken = Request.Cookies["refreshToken"];
            if (refreshToken == null)
            {
                return Unauthorized(new { Message = "Unauthorized to perform this request.", Code = 76 });
            }
            var user = FindUserByRefreshToken(refreshToken, Request.Headers[HeaderNames.UserAgent].ToString());


            if (user == null)
            {
                return Unauthorized(new { Message = "Unauthorized to perform this request.", Code = 76 });
            }

            // Check if the new birthdate is valid
            if (newBirthDay > DateTime.UtcNow)
            {
                return BadRequest(new { Message = "Invalid birthdate.", Code = 9 });
            }

            if (newBirthDay == user.Birthdate)
            {
                return BadRequest(new { Message = "Birthdate is the same as the current one.", Code = 15 });
            }

            user.Birthdate = newBirthDay;
            await _dBContext.SaveChangesAsync();

            // Add Content-Range header
            Response.Headers.Add("Content-Range", $"users 0-0/1");

            return Ok(new { Message = "Birthdate updated successfully.", Code = 84 });
        }

        [HttpPatch("update/username"), Authorize(Roles = "User,Admin,SuperAdmin")]
        public async Task<IActionResult> UpdateUserUsername(string newUsername)
        {
            var refreshToken = Request.Cookies["refreshToken"];
            if (refreshToken == null)
            {
                return Unauthorized(new { Message = "Unauthorized to perform this request.", Code = 76 });
            }
            var user = FindUserByRefreshToken(refreshToken, Request.Headers[HeaderNames.UserAgent].ToString());
            if (user == null)
            {
                return Unauthorized(new { Message = "Unauthorized to perform this request.", Code = 76 });
            }

            if (newUsername == null)
            {
                   return BadRequest(new { Message = "Username cannot be empty.", Code = 12 });
            }

            if (_dBContext.Users.Any(u => u.Username == newUsername))
            {
                return BadRequest(new { Message = "Username already in use.", Code = 8 });
            }

            user.Username = newUsername;
            await _dBContext.SaveChangesAsync();

            // Add Content-Range header
            Response.Headers.Add("Content-Range", $"users 0-0/1");

            return Ok(new { Message = "Username updated successfully.", Code = 82 });

        }

        [HttpPatch("update/fullname"), Authorize(Roles = "User,Admin,SuperAdmin")]
        public async Task<IActionResult> UpdateUserFullName(string newName)
        {
            var refreshToken = Request.Cookies["refreshToken"];
            if (refreshToken == null)
            {
                return Unauthorized(new { Message = "Unauthorized to perform this request.", Code = 76 });
            }
            var user = FindUserByRefreshToken(refreshToken, Request.Headers[HeaderNames.UserAgent].ToString());
            if (user == null)
            {
                return BadRequest(new { Message = "User not found.", Code = 36 });
            }

            if (newName == null)
            {
                return BadRequest(new { Message = "Full name cannot be empty.", Code = 16 });
            }

            user.FullName = newName;
            await _dBContext.SaveChangesAsync();

            // Add Content-Range header
            Response.Headers.Add("Content-Range", $"users 0-0/1");

            return Ok(new { Message = "Full name updated successfully.", Code = 83 });
        }

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

        // Find the user by giving a refresh token
        [NonAction]
        public User FindUserByRefreshToken(string refreshToken, string userAgent)
        {
            var currentTime = DateTime.UtcNow;

            var user = _dBContext.refreshTokens.FirstOrDefault(r => r.Token == refreshToken && r.Expires > currentTime && r.UserAgent == userAgent);

            if (user == null)
            {
                return null;
            }

            return _dBContext.Users.FirstOrDefault(u => u.Id == user.UserId);

        }

    }
}
