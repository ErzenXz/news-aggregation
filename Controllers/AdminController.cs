using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewsAggregation.Data;
using NewsAggregation.Models;
using NewsAggregation.Models.Security;
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

        public AdminController(ILogger<UserController> logger, DBContext dBContext)
        {
            _logger = logger;
            _dBContext = dBContext;
        }

        [HttpGet("all",Name = "admins/all")]
        public async Task<IActionResult> GetAdmins()
        {
            try
            {
                var admins = await _dBContext.Users.Where(u => u.Role == "Admin" || u.Role == "SuperAdmin").ToListAsync();

                return Ok(admins);
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
                var admin = await _dBContext.Users.FirstOrDefaultAsync(u => u.Id == id && (u.Role == "Admin" || u.Role == "SuperAdmin"));

                if (admin == null)
                {
                    return NotFound(new {Code= 61, Message = $"Admin with Id {id} not found."});
                }

                admin.Role = "User";

                _dBContext.Users.Update(admin);
                await _dBContext.SaveChangesAsync();

                return Ok(new {Code= 401, Message = $"Admin with Id {id} was demoted succesfuly!"});
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
                user.Role = "Admin";
                await _dBContext.Users.AddAsync(user);
                await _dBContext.SaveChangesAsync();

                return Ok(new {Code=402, Message= "Admin created successfully" });
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
                var users = await _dBContext.Users.Where(u => u.Role == "User").ToListAsync();

                return Ok(users);
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
                var userToUpdate = await _dBContext.Users.FirstOrDefaultAsync(u => u.Id == id && u.Role == "User");

                if (userToUpdate == null)
                {
                    return NotFound(new {Code = 36, Message = "User not found." });
                }

                userToUpdate.Email = user.Email;
                userToUpdate.Password = user.Password;

                await _dBContext.SaveChangesAsync();

                return Ok(userToUpdate);
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
                if (page < 0)
                {
                    return BadRequest(new { Message = "Invalid page number.", Code = 11 });
                }

                var ipMitigations = await _dBContext.ipMitigations.Skip(page * 10).Take(10).ToListAsync();

                return Ok(ipMitigations);
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
                var ipMitigation = await _dBContext.ipMitigations.FindAsync(id);

                if (ipMitigation == null)
                {
                    return NotFound(new {Code=403, Message="No IP's are currently blocked"});
                }

                return Ok(ipMitigation);
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
                var ipMitigationToUpdate = await _dBContext.ipMitigations.FindAsync(id);

                if (ipMitigationToUpdate == null)
                {
                    return NotFound(new { Code = 404, Message = "IP not found." });
                }

                ipMitigationToUpdate.BlockedUntil = ipMitigation.BlockedUntil;

                await _dBContext.SaveChangesAsync();

                return Ok(ipMitigationToUpdate);
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
                var ipMitigation = await _dBContext.ipMitigations.FindAsync(id);

                if (ipMitigation == null)
                {
                    return NotFound(new { Code = 404, Message = "IP not found." });
                }

                _dBContext.ipMitigations.Remove(ipMitigation);
                await _dBContext.SaveChangesAsync();

                return Ok(new { Code = 405, Message = "IP not unblocked succesfuly." });
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
                await _dBContext.ipMitigations.AddAsync(ipMitigation);
                await _dBContext.SaveChangesAsync();

                return CreatedAtAction(nameof(GetIpMitigation), new { id = ipMitigation.Id }, ipMitigation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating ipMitigation");
                return StatusCode(500, "An error occurred while processing the request.");
            }
        }

        double GetCpuUsage()
        {
            var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            cpuCounter.NextValue();
            System.Threading.Thread.Sleep(1000);
            return cpuCounter.NextValue();
        }


        [HttpGet("status"), AllowAnonymous]
        public IActionResult GetStatus()
        {

            // Get info about the server running the API

            var serverInfo = new
            {
                ServerName = Environment.MachineName,
                ServerTime = DateTime.Now,
                ServerTimeZone = TimeZoneInfo.Local.DisplayName,
                ServerOS = Environment.OSVersion.VersionString,
                ServerFramework = RuntimeInformation.FrameworkDescription,
                ServerRuntime = RuntimeInformation.OSDescription,
                ServerArchitecture = RuntimeInformation.OSArchitecture,
                ServerProcessors = Environment.ProcessorCount,
                ServerMemory = Environment.WorkingSet,
                ServerVersion = Environment.Version,
                Threads = Process.GetCurrentProcess().Threads.Cast<ProcessThread>().Select(t => new
                {
                    t.Id,
                    t.ThreadState,
                    t.StartTime,
                    t.TotalProcessorTime
                }).ToList(),
                MemoryMaped = Environment.WorkingSet.ToString(),
                ServerUptime = TimeSpan.FromMilliseconds(Environment.TickCount64),
                ServerCulture = CultureInfo.CurrentCulture.DisplayName,
                ServerIp = Dns.GetHostAddresses(Dns.GetHostName()).Where(ip => ip.AddressFamily == AddressFamily.InterNetwork).FirstOrDefault().ToString(),
                ServerHostname = Dns.GetHostName(),
                ServerDomain = Environment.UserDomainName
            };

            return Ok(new
            {
                status = "ok",
                version = "beta-1.0",
                server = serverInfo
            });

        }

        [HttpGet("cpu"), AllowAnonymous]
        public IActionResult GetCpu()
        {
            return Ok(new { usage= GetCpuUsage() });
        }


    }
}
