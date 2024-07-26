namespace NewsAggregation.Services.Interfaces
{
    public interface IUserValidationService
    {
        Task<bool> ValidateUserAsync(string userName, string email, string role);

    }
}
