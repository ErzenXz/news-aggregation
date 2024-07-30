using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using NewsAggregation.Data;
using NewsAggregation;
using NewsAggregation.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using NewsAggregation.Services;
using NewsAggregation.Models.Security;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using NewsAggregation.DTO;
using NewsAggregation.Services.Interfaces;
using System.Linq.Expressions;

namespace PersonalPodcast.Controllers
{
    [Route("auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        public static User user = new User();

        private readonly IConfiguration _configuration;
        private readonly DBContext _dBContext;
        private readonly IAuthService _authService;


        public AuthController(IConfiguration configuration, DBContext dBContext, IAuthService authService) {
            _configuration = configuration;
            _dBContext = dBContext;
            _authService = authService;
        }


        [HttpPost("register")]
        public async Task<IActionResult> Register(UserRegisterRequest userRequest)
        {
            var response = await _authService.Register(userRequest);
            if (response != null)
            {
                await _authService.GenerateBackupCodes();
                return response;
            }
            else
            {
                return BadRequest(new { Message = "Error registering user.", Code = 1000 });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(UserRequest userRequest)
        {
           var response = await _authService.Login(userRequest);
            if (response != null)
            {
                return response;
            }
            else
            {
                return BadRequest(new { Message = "Error logging in.", Code = 1000 });
            }
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken()
        {

            var respose = await _authService.RefreshToken();
            if (respose != null)
            {
                return respose;
            }
            else
            {
                return BadRequest(new { Message = "Error refreshing token.", Code = 45 });
            }
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var response = await _authService.Logout();
            if (response != null)
            {
                return response;
            }
            else
            {
                return BadRequest(new { Message = "Error logging out.", Code = 1000 });
            }
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword( string? email, string? code = "request")
        {

           var response = await _authService.ForgotPassword( email, code);

           return response;

        }

        [HttpPost("change-password"), Authorize(Roles ="User,Admin,SuperAdmin")]
        public async Task<IActionResult> ChangePassword(ChangePasswordRequest changePasswordRequest)
        {
            var response = await _authService.ChangePassword(changePasswordRequest);
            if (response != null)
            {
                return response;
            }
            else
            {
                return BadRequest(new { Message = "Error changing password.", Code = 1000 });
            }
        }

        [HttpPost("verify-email")]
        public async Task<IActionResult> VerifyEmail(string code)
        {
            var response = await _authService.VerifyEmail(code);

            return response;

        }

        [HttpPost("setup-mfa"), Authorize(Roles = "User,Admin,SuperAdmin")]
        public async Task<IActionResult> SetupMfa(string code = "first")
        {
            var response = await _authService.SetupMfa(code);
            if (response != null)
            {
                return response;
            }
            else
            {
                return BadRequest(new { Message = "Error setting up MFA.", Code = 1000 });
            }
        }

        [HttpPost("verify-mfa")]
        public async Task<IActionResult> VerifyMfa(string email, string code)
        {
            var response = await _authService.VerifyMfa(email,code);
            if (response != null)
            {
                return response;
            }
            else
            {
                return BadRequest(new { Message = "Error verifying MFA.", Code = 1000 });
            }
        }

        [HttpPost("generate-backup-codes"), Authorize(Roles = "User,Admin,SuperAdmin")]
        public async Task<IActionResult> GenerateBackupCodes()
        {
            var response = await _authService.GenerateBackupCodes();
            if (response != null)
            {
                return response;
            }
            else
            {
                return BadRequest(new { Message = "Error generating backup codes.", Code = 1000 });
            }
        }

        [HttpGet("info")]
        public async Task<IActionResult> GetUser()
        {
            try
            {

            var user = await _authService.GetUser();

            if (user != null)
            {
                return user;
            }
            else
            {
                return BadRequest(new { Message = "Error getting user info.", Code = 1000 });
            }
            }
            catch (Exception e)
            {
                return BadRequest(new { Message = "Error getting user info. " + e.Message, Code = 1000 });
            }
        }

        [HttpPost("login-{provider}")]
        public async Task<IActionResult> LoginProvider(string provider)
        {
            var response = await _authService.LoginProvider(HttpContext, provider);
            if (response != null)
            {
                return response;
            }
            else
            {
                return BadRequest(new { Message = "Error logging in with provider.", Code = 1000 });
            }
        }

        [HttpPost("external-login-callback")]
        public async Task<IActionResult> LoginProviderCallback(string provider)
        {
            var response = await _authService.LoginProviderCallback(HttpContext, provider);
            if (response != null)
            {
                return response;
            }
            else
            {
                return BadRequest(new { Message = "Error logging in with provider.", Code = 1000 });
            }
        }


    }

}
