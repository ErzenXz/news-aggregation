using Microsoft.AspNetCore.Mvc;
using NewsAggregation.DTO;
using NewsAggregation.Models;
using ServiceStack;

namespace NewsAggregation.Services.Interfaces
{
    public interface IAuthService : IService
    {
        public Task<IActionResult> Login(UserRequest userRequest);
        public Task<IActionResult> Register(UserRegisterRequest userRequest);
        public Task<IActionResult> RefreshToken();
        public Task<IActionResult> Logout();
        public Task<IActionResult> ForgotPassword(UserRequest userRequest, string? emailRq, string? code, int? verifyRequest);
        public Task<IActionResult> ChangePassword(ChangePasswordRequest changePasswordRequest);
        public Task<IActionResult> VerifyEmail(string code);
        public Task<IActionResult> SendVerifyEmail();
        public Task<IActionResult> GetUser();
        public Task<IActionResult> SetupMfa(string code = "first");
        public Task<IActionResult> VerifyMfa(string email, string code);
        public Task<IActionResult> GenerateBackupCodes();


        public string CreateAccessToken(User user);
        public void SetCookies(string refreshToken);
        public string GetUserIp();
        public string GenerateRefreshToken();
    }
}
