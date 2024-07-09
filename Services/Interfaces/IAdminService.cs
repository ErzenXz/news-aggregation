using Microsoft.AspNetCore.Mvc;
using NewsAggregation.Models;
using NewsAggregation.Models.Security;
using ServiceStack;

namespace NewsAggregation.Services.Interfaces
{
    public interface IAdminService : IService
    {
        public Task<IActionResult> GetAdmins();
        public  Task<IActionResult> DeleteAdmin(Guid id);
        public  Task<IActionResult> CreateAdmin([FromBody] User user);
        public Task<IActionResult> GetUsers();
        public Task<IActionResult> UpdateUser(Guid id, [FromBody] User user);
        public Task<IActionResult> GetIpMitigations(int page = 1);
        public Task<IActionResult> GetIpMitigation(int id);
        public Task<IActionResult> UpdateIpMitigation(int id, [FromBody] IpMitigations ipMitigation);
        public Task<IActionResult> DeleteIpMitigation(int id);
        public Task<IActionResult> CreateIpMitigation([FromBody] IpMitigations ipMitigation);
        public Task<IActionResult> GetStatus();
        public string FormatThreadState(int value);

    }
}
