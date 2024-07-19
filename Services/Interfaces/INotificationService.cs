using Microsoft.AspNetCore.Mvc;
using ServiceStack;

namespace NewsAggregation.Services.Interfaces
{
    public interface INotificationService : IService
    {
        public Task<IActionResult> SendNotification(string? title, string? message);

    }
}
