using Microsoft.AspNetCore.Mvc;
using NewsAggregation.DTO;

namespace NewsAggregation.Services.Interfaces
{
    public interface IUserService
    {
        public Task<IActionResult> GetActiveSessions();
        public Task<IActionResult> RevokeActiveSession(string? ipAdress, string userAgent);
        public Task<IActionResult> UpdateUser(UserUpdateRequest userUpdateRequest);
        public Task<IActionResult> UpdateUserBirthdate(DateTime newBirthDay);
        public Task<IActionResult> UpdateUserUsername(string newUsername);
        public Task<IActionResult> UpdateUserFullName(string newName);
        public Task<IActionResult> UpdateProfileImage(string imageUrl);
        public Task<IActionResult> GetViewHistory(string? range = null);
        public Task<IActionResult> GetSavedArticles(string? range = null);
    }
}
