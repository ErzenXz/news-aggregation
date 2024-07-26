using Microsoft.EntityFrameworkCore;
using NewsAggregation.Data;
using NewsAggregation.Services.Interfaces;

namespace NewsAggregation.Services
{
    public class UserValidationService : IUserValidationService
    {
        private readonly DBContext _context;

        public UserValidationService(DBContext context)
        {
            _context = context;
        }

        public async Task<bool> ValidateUserAsync(string userName, string email, string role)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == userName && u.Email == email && u.Role == role);

            return user != null;
        }
    }
}
