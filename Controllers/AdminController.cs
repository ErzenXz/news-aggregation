using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewsAggregation.Data;
using NewsAggregation.Models;
using NewsAggregation.Models.Security;
using NewsAggregation.Services.Interfaces;
using NewsAggregation.Services.ServiceJobs.Email;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace NewsAggregation.Controllers
{
    [ApiController]
    [Route("admin"), Authorize(Roles = "Admin,SuperAdmin")]
    public class AdminController : ControllerBase
    {
        private readonly ILogger<UserController> _logger;
        private readonly DBContext _dBContext;
        private readonly IAdminService _adminService;

        public AdminController(ILogger<UserController> logger, DBContext dBContext, IAdminService adminService)
        {
            _logger = logger;
            _dBContext = dBContext;
            _adminService = adminService;
        }

        [HttpGet("all",Name = "admins/all")]
        public async Task<IActionResult> GetAdmins()
        {
            try
            {
                return await _adminService.GetAdmins();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching admins");
                return StatusCode(500, "An error occurred while processing the request.");
            }
        }

        

        [HttpDelete("remove/{id}",Name ="admins/remove/{id}")]
        public async Task<IActionResult> DeleteAdmin(Guid id)
        {
            try
            {
                return await _adminService.DeleteAdmin(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting admin");
                return StatusCode(500, "An error occurred while processing the request.");
            }
        }

        [HttpPost("create",Name ="admins/create")]
        public async Task<IActionResult> CreateAdmin([FromBody] User user)
        {
            try
            {
                return await _adminService.CreateAdmin(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating admin");
                return StatusCode(500, "An error occurred while processing the request.");
            }
        }

        // Path user

        [HttpGet("users/all", Name ="users/all")]
        public async Task<IActionResult> GetUsers()
        {
            try
            {
                var users = await _adminService.GetUsers();
                return users;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching users");
                return StatusCode(500, "An error occurred while processing the request.");
            }
        }

        [HttpPut("users/{id}",Name ="users/{id}")]
        public async Task<IActionResult> UpdateUser(Guid id, [FromBody] User user)
        {
            try
            {
                return await _adminService.UpdateUser(id, user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating user");
                return StatusCode(500, "An error occurred while processing the request.");
            }
        }

        // Path ipMitigation

        [HttpGet("security/blocked", Name = "security/blocked")]
        public async Task<IActionResult> GetIpMitigations(int page = 1)
        {
            try
            {
                return await _adminService.GetIpMitigations(page);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching ipMitigations");
                return StatusCode(500, "An error occurred while processing the request.");
            }
        }

        [HttpGet("security/blocked/{id}", Name = "security/blocked/{id}")]
        public async Task<IActionResult> GetIpMitigation(int id)
        {
            try
            {
                return await _adminService.GetIpMitigation(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching ipMitigation");
                return StatusCode(500, "An error occurred while processing the request.");
            }
        }

        [HttpPut("security/recheck/{id}", Name = "security/recheck/{id}")]
        public async Task<IActionResult> UpdateIpMitigation(int id, [FromBody] IpMitigations ipMitigation)
        {
            try
            {
                return await _adminService.UpdateIpMitigation(id, ipMitigation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating ipMitigation");
                return StatusCode(500, "An error occurred while processing the request.");
            }
        }

        [HttpDelete("security/unblock/{id}", Name = "security/unblock/{id}")]
        public async Task<IActionResult> DeleteIpMitigation(int id)
        {
            try
            {
                return await _adminService.DeleteIpMitigation(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting ipMitigation");
                return StatusCode(500, "An error occurred while processing the request.");
            }
        }

        [HttpPost("security/block", Name = "security/block")]
        public async Task<IActionResult> CreateIpMitigation([FromBody] IpMitigations ipMitigation)
        {
            try
            {
                return await _adminService.CreateIpMitigation(ipMitigation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating ipMitigation");
                return StatusCode(500, "An error occurred while processing the request.");
            }
        }


        [HttpGet("status"), AllowAnonymous]
        public async Task<IActionResult> GetStatus()
        {
            try
            {
                return await _adminService.GetStatus();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching status");
                return StatusCode(500, "An error occurred while processing the request.");
            }
        }

    }
}
