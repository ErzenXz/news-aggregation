using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewsAggregation.Models.Security;
using NewsAggregation.Models;
using NewsAggregation.Services.Interfaces;
using ServiceStack;
using System.Diagnostics;
using System.Globalization;
using System.Net.Sockets;
using System.Net;
using System.Runtime.InteropServices;
using NewsAggregation.Data;
using Microsoft.EntityFrameworkCore;

namespace NewsAggregation.Services
{
    public class AdminService : IAdminService
    {


        private readonly DBContext _dBContext;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;

        public AdminService(DBContext dBContext, IHttpContextAccessor httpContextAccessor, IConfiguration configuration, ILogger<AuthService> logger )
        {
            _dBContext = dBContext;
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<IActionResult> GetAdmins()
        {
            try
            {
                var admins = await _dBContext.Users.Where(u => u.Role == "Admin" || u.Role == "SuperAdmin").ToListAsync();

                return new OkObjectResult(admins);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching admins");
                return new BadRequestObjectResult(new { Message = "An error occurred while processing the request.", Code = 1000 });
            }
        }


        public async Task<IActionResult> DeleteAdmin(Guid id)
        {
            try
            {
                var admin = await _dBContext.Users.FirstOrDefaultAsync(u => u.Id == id && (u.Role == "Admin" || u.Role == "SuperAdmin"));

                if (admin == null)
                {
                    return new NotFoundObjectResult(new { Code = 61, Message = $"Admin with Id {id} not found." });
                }

                admin.Role = "User";

                _dBContext.Users.Update(admin);
                await _dBContext.SaveChangesAsync();

                return new OkObjectResult(new { Code = 401, Message = $"Admin with Id {id} was demoted succesfuly!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting admin");
                return new BadRequestObjectResult(new { Message = "An error occurred while processing the request.", Code = 1000 });
            }
        }

        public async Task<IActionResult> CreateAdmin([FromBody] User user)
        {
            try
            {
                user.Role = "Admin";
                await _dBContext.Users.AddAsync(user);
                await _dBContext.SaveChangesAsync();

                return new OkObjectResult(new { Code = 402, Message = "Admin created successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating admin");
                return new BadRequestObjectResult(new { Message = "An error occurred while processing the request.", Code = 1000 });
            }
        }


        public async Task<IActionResult> GetUsers()
        {
            try
            {
                var users = await _dBContext.Users.Where(u => u.Role == "User").ToListAsync();

                return new OkObjectResult(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching users");
                return new BadRequestObjectResult(new { Message = "An error occurred while processing the request.", Code = 1000 });
            }
        }

        public async Task<IActionResult> UpdateUser(Guid id, [FromBody] User user)
        {
            try
            {
                var userToUpdate = await _dBContext.Users.FirstOrDefaultAsync(u => u.Id == id && u.Role == "User");

                if (userToUpdate == null)
                {
                    return new NotFoundObjectResult(new { Code = 36, Message = "User not found." });
                }

                userToUpdate.Email = user.Email;
                userToUpdate.Password = user.Password;

                await _dBContext.SaveChangesAsync();

                return new OkObjectResult(userToUpdate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating user");
                return new BadRequestObjectResult(new { Message = "An error occurred while processing the request.", Code = 1000 });
            }
        }

        // Path ipMitigation

        public async Task<IActionResult> GetIpMitigations(int page = 1)
        {
            try
            {
                if (page < 0)
                {
                    return new BadRequestObjectResult(new { Message = "Invalid page number.", Code = 11 });
                }

                var ipMitigations = await _dBContext.ipMitigations.Skip(page * 10).Take(10).ToListAsync();

                return new OkObjectResult(ipMitigations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching ipMitigations");
                return new BadRequestObjectResult(new { Message = "An error occurred while processing the request.", Code = 1000 });
            }
        }

        public async Task<IActionResult> GetIpMitigation(int id)
        {
            try
            {
                var ipMitigation = await _dBContext.ipMitigations.FindAsync(id);

                if (ipMitigation == null)
                {
                    return new NotFoundObjectResult(new { Code = 403, Message = "No IP's are currently blocked" });
                }

                return new OkObjectResult(ipMitigation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching ipMitigation");
                return new BadRequestObjectResult(new { Message = "An error occurred while processing the request.", Code = 1000 });
            }
        }

        public async Task<IActionResult> UpdateIpMitigation(int id, [FromBody] IpMitigations ipMitigation)
        {
            try
            {
                var ipMitigationToUpdate = await _dBContext.ipMitigations.FindAsync(id);

                if (ipMitigationToUpdate == null)
                {
                    return new NotFoundObjectResult(new { Code = 404, Message = "IP not found." });
                }

                ipMitigationToUpdate.BlockedUntil = ipMitigation.BlockedUntil;

                await _dBContext.SaveChangesAsync();

                return new OkObjectResult(ipMitigationToUpdate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating ipMitigation");
                return new BadRequestObjectResult(new { Message = "An error occurred while processing the request.", Code = 1000 });
            }
        }

        public async Task<IActionResult> DeleteIpMitigation(int id)
        {
            try
            {
                var ipMitigation = await _dBContext.ipMitigations.FindAsync(id);

                if (ipMitigation == null)
                {
                    return new NotFoundObjectResult(new { Code = 404, Message = "IP not found." });
                }

                _dBContext.ipMitigations.Remove(ipMitigation);
                await _dBContext.SaveChangesAsync();

                return new OkObjectResult(new { Code = 405, Message = "IP not unblocked succesfuly." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting ipMitigation");
                return new BadRequestObjectResult(new { Message = "An error occurred while processing the request.", Code = 1000 });
            }
        }

        public async Task<IActionResult> CreateIpMitigation([FromBody] IpMitigations ipMitigation)
        {
            try
            {
                await _dBContext.ipMitigations.AddAsync(ipMitigation);
                await _dBContext.SaveChangesAsync();

                //return CreatedAtAction(nameof(GetIpMitigation), new { id = ipMitigation.Id }, ipMitigation);
                return new OkObjectResult(new { Code = 1000, Message = "IP blocked successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating ipMitigation");
                return new BadRequestObjectResult(new { Message = "An error occurred while processing the request.", Code = 1000 });
            }
        }

        public string FormatThreadState(int value)
        {
            switch (value)
            {
                case 0:
                    return "Initialized";
                case 1:
                    return "Ready";
                case 2:
                    return "Running";
                case 3:
                    return "Standby";
                case 4:
                    return "Terminated";
                case 5:
                    return "Wait";
                case 6:
                    return "Transition";
                case 7:
                    return "Unknown";
                default:
                    return "Invalid";
            }
        }

        public Task<IActionResult> GetStatus()
        {

            var serverInfo = new
            {
                ServerName = Environment.MachineName,
                ServerTime = DateTime.UtcNow,
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
                    ThreadStateFormated = FormatThreadState((int)t.ThreadState),
                    t.StartTime,
                    t.TotalProcessorTime,
                    t.PriorityLevel,
                }).ToList(),
                MemoryMaped = Environment.WorkingSet.ToString(),
                ServerUptime = TimeSpan.FromMilliseconds(Environment.TickCount64),
                ServerCulture = CultureInfo.CurrentCulture.DisplayName,
                ServerIp = Dns.GetHostAddresses(Dns.GetHostName()).Where(ip => ip.AddressFamily == AddressFamily.InterNetwork).FirstOrDefault().ToString(),
                ServerHostname = Dns.GetHostName(),
                ServerDomain = Environment.UserDomainName
            };

            return Task.FromResult<IActionResult>(new OkObjectResult(new
            {
                status = "ok",
                version = "beta-1.1.1",
                server = serverInfo
            }));

        }
    }
}
