using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using NewsAggregation.DTO;
using NewsAggregation.Models.Security;
using NewsAggregation.Models;
using NewsAggregation.Services.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using NewsAggregation.Data;
using Microsoft.EntityFrameworkCore;
using OtpNet;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using NewsAggregation.Services.ServiceJobs.Email;
using NewsAggregation.Helpers;

namespace NewsAggregation.Services
{
    public class AuthService : IAuthService
    {

        private readonly DBContext _dBContext;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;
        private readonly EmailQueueService _emailQueueService;


        public AuthService(DBContext dbContext, IHttpContextAccessor httpContextAccessor, IConfiguration configuration, EmailQueueService emailQueueService)
        {
            _dBContext = dbContext;
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
            _emailQueueService = emailQueueService;
        }

        public async Task<IActionResult> Register(UserRegisterRequest userRequest)
        {
            var user = new User();

            if (userRequest.Email == null || userRequest.Password == null)
            {
                return new BadRequestObjectResult(new { Message = "Email and password are required.", Code = 3 });
            }

            if (userRequest.Email.Length < 5 || userRequest.Email.Length > 100)
            {
                return new BadRequestObjectResult(new { Message = "Email must be between 5 and 100 characters.", Code = 4 });
            }

            if (userRequest.Password.Length < 8 || userRequest.Password.Length > 100)
            {
                return new BadRequestObjectResult(new { Message = "Password must be between 8 and 100 characters.", Code = 5 });
            }

            if (_dBContext.Users.Any(u => u.Email == userRequest.Email.ToLower()))
            {
                return new BadRequestObjectResult(new { Message = "Email already in use.", Code = 7 });
            }

            if (userRequest.Birthdate == null)
            {
                return new BadRequestObjectResult(new { Message = "Birthdate is required.", Code = 6 });
            }

            if (_dBContext.Users.Any(u => u.Username == userRequest.Username.ToLower()))
            {
                return new BadRequestObjectResult(new { Message = "Username already in use.", Code = 8 });
            }

            string passwordHashed = BCrypt.Net.BCrypt.HashPassword(userRequest.Password);
            string email = userRequest.Email.ToLower();

            var refreshToken = GenerateRefreshToken();

            // Check if the user ip is blocked
            var ip = GetUserIp();
            var ipBlock = _dBContext.ipMitigations.Where(i => i.IpAddress == ip).OrderByDescending(i => i.BlockedUntil).FirstOrDefault();
            if (ipBlock != null)
            {
                if (ipBlock.BlockedUntil > DateTime.UtcNow)
                {
                    return new UnauthorizedObjectResult(new { Message = "Our system has detected multiple login attempts from your IP address, which is a violation of our Terms of Service. As a result, access from your IP has been temporarily blocked for 10 minutes. This measure helps protect our platform from unauthorized access and ensures a secure environment for all users.", Code = 44 });
                }
            }

            user.Id = Guid.NewGuid();
            user.Email = email.ToLower();
            user.Password = passwordHashed;
            user.FirstLogin = DateTime.UtcNow;
            user.LastLogin = DateTime.UtcNow;
            user.ConnectingIp = GetUserIp();
            user.Birthdate = userRequest.Birthdate;
            user.Role = "User";
            user.Username = userRequest.Username.ToLower();
            user.FullName = userRequest.FullName;
            user.Language = userRequest.Language;
            user.TimeZone = userRequest.TimeZone;
            user.TokenVersion = 1;
            user.ProfilePicture = "https://ui-avatars.com/api/?name=" + Uri.EscapeDataString(userRequest.FullName) + "&background=random&color=fff&rounded=true";

            // Create a new refresh token
            var refreshTokenObj = new RefreshTokens
            {
                UserId = user.Id,
                Token = refreshToken,
                Expires = DateTime.UtcNow.AddDays(7),
                TokenVersion = user.TokenVersion,
                Created = DateTime.UtcNow,
                CreatedByIp = GetUserIp(),
                UserAgent = _httpContextAccessor.HttpContext.Request.Headers["User-Agent"].ToString(),
                DeviceName = "Unknown"
            };

            // Add failed login attempt to the AuthLogs table

            var authLog = new AuthLogs();
            authLog.Email = userRequest.Email;
            authLog.IpAddress = ip;
            authLog.UserAgent = _httpContextAccessor.HttpContext.Request.Headers["User-Agent"].ToString();
            authLog.Date = DateTime.UtcNow;
            authLog.Result = "Register";


            SetCookies(refreshToken);

            // Save user to database
            _dBContext.authLogs.Add(authLog);
            _dBContext.Users.Add(user);
            _dBContext.refreshTokens.Add(refreshTokenObj);
            await _dBContext.SaveChangesAsync();

            // Send verify email to user

            await SendVerifyEmail();


            return new OkObjectResult(new { Message = "User registered successfully!", Code = 43 });
        }

        public async Task<IActionResult> Login(UserRequest userRequest)
        {

            if (userRequest.Email == null || userRequest.Password == null)
            {
                return new BadRequestObjectResult(new { Message = "Email and password are required.", Code = 3 });
            }

            if (userRequest.Email.Length < 5 || userRequest.Email.Length > 100)
            {
                return new BadRequestObjectResult(new { Message = "Email must be between 5 and 100 characters.", Code = 4 });
            }

            if (userRequest.Password.Length < 8 || userRequest.Password.Length > 100)
            {
                return new BadRequestObjectResult(new { Message = "Pasword is required.", Code = 2 });
            }


            // Check if the user ip is blocked
            var ip = GetUserIp();
            var userAgent = _httpContextAccessor.HttpContext.Request.Headers["User-Agent"].ToString();
            var ipBlock = _dBContext.ipMitigations.Where(i => i.IpAddress == ip).OrderByDescending(i => i.BlockedUntil).FirstOrDefault();

            if (ipBlock != null)
            {
                if (ipBlock.BlockedUntil > DateTime.UtcNow)
                {
                    return new UnauthorizedObjectResult(new { Message = "Our system has detected multiple login attempts from your IP address, which is a violation of our Terms of Service. As a result, access from your IP has been temporarily blocked for 10 minutes. This measure helps protect our platform from unauthorized access and ensures a secure environment for all users.", Code = 44 });
                }
            }

            // Check if user exists in database
            var user = _dBContext.Users.FirstOrDefault(u => u.Email == userRequest.Email.ToLower());
            if (user == null)
            {
                return new BadRequestObjectResult(new { Message = "User not found.", Code = 36 });
            }


            if (!BCrypt.Net.BCrypt.Verify(userRequest.Password, user.Password))
            {

                // Add failed login attempt to database

                var userId = _dBContext.Users.FirstOrDefault(u => u.Email == userRequest.Email).Id;

                DateTime currentTime = DateTime.UtcNow;
                DateTime oneHourAgo = currentTime.AddMinutes(-20).AddMilliseconds(-currentTime.Millisecond);

                var failedLoginAttempts = _dBContext.accountSecurity
                    .Where(a => a.UserId == userId && a.IpAddress == ip && a.LastFailedLogin >= oneHourAgo)
                    .OrderByDescending(a => a.LastFailedLogin)
                    .FirstOrDefault();


                if (failedLoginAttempts != null)
                {
                    failedLoginAttempts.FailedLoginAttempts += 1;
                    failedLoginAttempts.LastFailedLogin = DateTime.UtcNow;
                    _dBContext.accountSecurity.Update(failedLoginAttempts);
                    await _dBContext.SaveChangesAsync();

                    // Add failed login attempt to the AuthLogs table

                    var authLog = new AuthLogs();
                    authLog.Email = userRequest.Email;
                    authLog.IpAddress = ip;
                    authLog.UserAgent = userAgent;
                    authLog.Date = DateTime.UtcNow;
                    authLog.Result = "Failed";

                    await _dBContext.authLogs.AddAsync(authLog);


                    if (failedLoginAttempts.FailedLoginAttempts > 5)
                    {
                        var ipMitigation = new IpMitigations();
                        ipMitigation.IpAddress = ip;
                        ipMitigation.BlockedUntil = DateTime.UtcNow.AddMinutes(10);


                        // Remove all other failed login attempts from the same IP
                        _dBContext.ipMitigations.RemoveRange(_dBContext.ipMitigations.Where(a => a.IpAddress == ip));

                        _dBContext.ipMitigations.Add(ipMitigation);
                        await _dBContext.SaveChangesAsync();



                        return new UnauthorizedObjectResult(new { Message = "Our system has detected multiple login attempts from your IP address, which is a violation of our Terms of Service. As a result, access from your IP has been temporarily blocked for 10 minutes. This measure helps protect our platform from unauthorized access and ensures a secure environment for all users.", Code = 44 });
                    }

                }
                else
                {
                    // Add failed login attempt to the AuthLogs table

                    var authLog = new AuthLogs();
                    authLog.Email = userRequest.Email;
                    authLog.IpAddress = ip;
                    authLog.UserAgent = userAgent;
                    authLog.Date = DateTime.UtcNow;
                    authLog.Result = "Failed";

                    await _dBContext.authLogs.AddAsync(authLog);

                    AccountSecurity accountSecurity = new AccountSecurity();
                    accountSecurity.UserId = userId;
                    accountSecurity.IpAddress = ip;
                    accountSecurity.FailedLoginAttempts = 1;
                    accountSecurity.LastFailedLogin = DateTime.UtcNow;
                    _dBContext.accountSecurity.Add(accountSecurity);
                    await _dBContext.SaveChangesAsync();
                }

                return new BadRequestObjectResult(new { Message = "Invalid password.", Code = 37 });
            }


            string accessToken = CreateAccessToken(user);

            if (accessToken != null)
            {
                // Add success login attempt to the AuthLogs table

                var authLog = new AuthLogs();
                authLog.Email = userRequest.Email;
                authLog.IpAddress = ip;
                authLog.UserAgent = userAgent;
                authLog.Date = DateTime.UtcNow;
                authLog.Result = "Success";

                await _dBContext.authLogs.AddAsync(authLog);

                // Check if the user's refresh token has expired

                // Find all refresh tokens for the user
                var refreshTokens = _dBContext.refreshTokens.Where(r => r.UserId == user.Id).ToList();

                var currentRefreshTokenVersion = user.TokenVersion;

                string newRefreshToken = GenerateRefreshToken();

                var found = false;

                // Check if any of the refresh tokens are still active

                foreach (var token in refreshTokens)
                {
                    if (currentRefreshTokenVersion == token.TokenVersion && token.IsActive && userAgent == token.UserAgent)
                    {
                        // If the token is active, update the last used time
                        token.LastUsed = DateTime.UtcNow;
                        _dBContext.refreshTokens.Update(token);
                        await _dBContext.SaveChangesAsync();
                        found = true;
                        newRefreshToken = token.Token;
                    } 
                }


                if (!found)
                {
                    // If no active refresh token was found, generate a new one
                    var refreshToken = new RefreshTokens
                    {
                        UserId = user.Id,
                        Token = newRefreshToken,
                        Expires = DateTime.UtcNow.AddDays(7),
                        TokenVersion = user.TokenVersion,
                        Created = DateTime.UtcNow,
                        CreatedByIp = ip,
                        UserAgent = userAgent,
                        DeviceName = "Unknown"
                    };

                    _dBContext.refreshTokens.Add(refreshToken);
                    found = true;
                    await _dBContext.SaveChangesAsync();
                }


                user.LastLogin = DateTime.UtcNow;

                var oldConIP = user.ConnectingIp;
                user.ConnectingIp = GetUserIp();

                _dBContext.Users.Update(user);
                await _dBContext.SaveChangesAsync();


                if(user.IsTwoFactorEnabled)
                {
                    return new OkObjectResult(new { Message = "MFA is required", Code = 1000 });
                } else
                {
                    SetCookies(newRefreshToken);

                if (oldConIP != GetUserIp())
                {

                        EmailMessage emailMessage = new()
                        {
                            From = "noreply.sapientia.life",
                            To = user.Email,
                            Subject = "New Login from New IP on Sapientia Life",
                            Body = EmailTemplates.IP_LOGGED_FROM_NEW_LOCATION
                        };

                        _emailQueueService.QueueEmail(emailMessage);

                    }
                    return new OkObjectResult(new { Message = "User logged in successfully!", Code = 38, AccessToken = accessToken, newRefreshToken });
                }
            }
            else
            {
                return new BadRequestObjectResult(new { Message = "Error creating access token.", Code = 39 });
            }

        }

        public async Task<IActionResult> Logout()
        {
            var httpContex = _httpContextAccessor.HttpContext;

            var refreshToken = httpContex.Request.Cookies["refreshToken"];

            if (refreshToken == null)
            {
                return new UnauthorizedObjectResult(new { Message = "No refresh token found or refresh token has expired.", Code = 40 });
            }

            var user = await FindUserByRefreshToken(refreshToken, httpContex.Request.Headers["User-Agent"].ToString());
            var userAgent = httpContex.Request.Headers["User-Agent"].ToString();

            if (user == null)
            {
                return new UnauthorizedObjectResult(new { Message = "Invalid refresh token or refresh token has expired.", Code = 41 });
            }
            var currentTime = DateTime.UtcNow;

            // Revoke the refresh token
            var refreshTokenOBJ = _dBContext.refreshTokens.FirstOrDefault(r => r.Token == refreshToken && r.Expires > currentTime && r.UserAgent == userAgent && r.Revoked == null);

            if (refreshTokenOBJ != null)
            {
                refreshTokenOBJ.Revoked = DateTime.UtcNow;
                refreshTokenOBJ.RevokedByIp = GetUserIp();
                refreshTokenOBJ.UserAgent = userAgent;
                refreshTokenOBJ.RevocationReason = "User logged out";
                _dBContext.refreshTokens.Update(refreshTokenOBJ);
                await _dBContext.SaveChangesAsync();
            }

            SetCookies("");

            return new OkObjectResult(new { Message = "User logged out successfully!", Code = 42 });
        }

        public async Task<IActionResult> ForgotPassword(string? email, string? code = "request")
        {

            if (code != "request")
            {
                var resetEmail = _dBContext.resetEmails.FirstOrDefault(r => r.Email == email && r.Code == code.ToString());
                if (resetEmail == null)
                {
                    return new BadRequestObjectResult(new { Message = "Invalid code.", Code = 46 });
                }

                if (resetEmail.ValidUntil < DateTime.UtcNow)
                {
                    return new BadRequestObjectResult(new { Message = "Code has expired.", Code = 47 });
                }

                // Generate new password
                string newPassword = Convert.ToBase64String(RandomNumberGenerator.GetBytes(7));

                // Hash the new password
                string newPasswordHashed = BCrypt.Net.BCrypt.HashPassword(newPassword);

                var user2 = _dBContext.Users.FirstOrDefault(u => u.Email == email);

                if (user2 == null)
                {
                    return new BadRequestObjectResult(new { Message = "User was not found.", Code = 36 });
                }

                user2.Password = newPasswordHashed;

                _dBContext.Users.Update(user2);
                await _dBContext.SaveChangesAsync();

                EmailMessage emailMessage = new()
                {
                    From = "noreply@sapientia.life",
                    To = email,
                    Subject = "New Password",
                    Body = $"<h1>Hello!</h1><br>You have requested to reset your password in Sapientia.<br>Here is your new password: <strong>" + newPassword + "</strong><br>Thanks!"
                };

                _emailQueueService.QueueEmail(emailMessage);

                return new OkObjectResult(new { Message = $"Here is your new generated password: {newPassword}, It was also send via email.", Code = 69 });

            }
            else
            {

                if (email == null)
                {
                    return new BadRequestObjectResult(new { Message = "Email is required.", Code = 1 });
                }

                if (email.Length < 5 || email.Length > 100)
                {
                    return new BadRequestObjectResult(new { Message = "Email must be between 5 and 100 characters.", Code = 3 });
                }

                // Check if user exists in database
                var user = _dBContext.Users.FirstOrDefault(u => u.Email == email);
                if (user == null)
                {
                    return new BadRequestObjectResult(new { Message = "User not found.", Code = 36 });
                }

                // Generate new code
                string randomCode = Guid.NewGuid().ToString().Substring(0, 8);


                var emailResetOp = new ResetEmail
                {
                    Email = email,
                    Code = randomCode,
                    CreatedDate = DateTime.UtcNow,
                    ValidUntil = DateTime.UtcNow.AddMinutes(15)

                };

                // Save to DB
                _dBContext.resetEmails.Add(emailResetOp);
                await _dBContext.SaveChangesAsync();

                // Send email with new password

                EmailMessage emailMessage = new()
                {
                    From = "noreply@sapientia.life",
                    To = email,
                    Subject = "Request to Reset Password",
                    Body = $"<h3>Hello {user.FullName}!</h3><br>You have requested to reset your password in PersonalPodcast.<br>Here is your one time reset code: <strong>" + randomCode + "</strong><br>" +
                    "<p>You can use this link to directly change your password</p> " +
                    $"<a href='https://api.sapientia.life/auth/forgot-password?email={user.Email}&code={randomCode}'>Reset password</a>" +
                    "Your link will expire in 15 minutes. If you did not request this, please ignore this email.<br>" +
                    "Thanks!"
                };

                _emailQueueService.QueueEmail(emailMessage);

                return new OkObjectResult(new { Message = "A code was send to your email. ", Code = 68 });
            }

        }

        public async Task<IActionResult> VerifyEmail(string code)
        {
            var httpContex = _httpContextAccessor.HttpContext;

            if (httpContex == null)
            {
                return new UnauthorizedObjectResult(new { Message = "No http context found.", Code = 1000 });
            }

            var refreshToken = httpContex.Request.Cookies["refreshToken"];
            var userAgent = httpContex.Request.Headers["User-Agent"].ToString();

            if (refreshToken == null)
            {
                return new UnauthorizedObjectResult(new { Message = "No refresh token found.", Code = 40 });
            }

            var user = await FindUserByRefreshToken(refreshToken, userAgent);

            if (user == null)
            {
                SetCookies("");
                return new UnauthorizedObjectResult(new { Message = "Invalid refresh token or refresh token has expired.", Code = 41 });
            }

            if (user.IsEmailVerified)
            {
                return new OkObjectResult(new { Message = "Email is already verified.", Code = 1000 });
            }

            var currentTime = DateTime.UtcNow; 
            var verifyEmail = _dBContext.verifyEmails.FirstOrDefault(v => v.Email == user.Email && v.Code == code && v.ValidUntil > currentTime);
            if (verifyEmail == null)
            {
                return new BadRequestObjectResult(new { Message = "Invalid code or code has expired.", Code = 203 });
            }

            user.IsEmailVerified = true;
            _dBContext.Users.Update(user);
            await _dBContext.SaveChangesAsync();

            return new OkObjectResult(new { Message = "Email verified successfully!", Code = 1000 });
        }

        public async Task<IActionResult> SendVerifyEmail()
        {
            var httpContex = _httpContextAccessor.HttpContext;

            if (httpContex == null)
            {
                return new UnauthorizedObjectResult(new { Message = "No http context found.", Code = 1000 });
            }

            var refreshToken = httpContex.Request.Cookies["refreshToken"];
            var userAgent = httpContex.Request.Headers["User-Agent"].ToString();

            if (refreshToken == null)
            {
                return new UnauthorizedObjectResult(new { Message = "No refresh token found.", Code = 40 });
            }

            var user = await FindUserByRefreshToken(refreshToken, userAgent);

            if (user == null)
            {
                SetCookies("");
                return new UnauthorizedObjectResult(new { Message = "Invalid refresh token or refresh token has expired.", Code = 41 });
            }

            if (user.IsEmailVerified)
            {
                return new OkObjectResult(new { Message = "Email is already verified.", Code = 1000 });
            }

            // Generate new code
            var randomCode = Guid.NewGuid().ToString().Substring(0, 8);

            var verifyEmail = new VerifyEmail
            {
                Email = user.Email,
                Code = randomCode,
                CreatedDate = DateTime.UtcNow,
                ValidUntil = DateTime.UtcNow.AddMinutes(15)
            };

            // Save to DB
            _dBContext.verifyEmails.Add(verifyEmail);
            await _dBContext.SaveChangesAsync();

            // Send email with new password

            EmailMessage emailMessage = new()
            {
                From = "noreply@sapientia.life",
                To = user.Email,
                Subject = "Verify Email",
                Body = $"<h3>Hello {user.FullName}!</h3><br>You have requested to verify your email in Sapientia.<br>Here is your one time verification code: <strong>" + randomCode + "</strong><br>" +
                               "<p>You can use this link to directly verify your email</p> " +
                                              $"<a href='https://api.sapientia.life/auth/verify-email?code={randomCode}'>Verify Email</a>" +
                                                             "Your link will expire in 15 minutes. If you did not request this, please ignore this email.<br>" +
                                                                            "Thanks!"
            };

            _emailQueueService.QueueEmail(emailMessage);

            return new OkObjectResult(new { Message = "Email verification code sent successfully!", Code = 1000 });
        }

        public async Task<IActionResult> ChangePassword(ChangePasswordRequest changePasswordRequest)
        {
            if (changePasswordRequest.OldPassword == null || changePasswordRequest.NewPassword == null)
            {
                return new BadRequestObjectResult(new { Message = "Old password and new password are required.", Code = 203 });
            }

            if (changePasswordRequest.OldPassword.Length < 8 || changePasswordRequest.OldPassword.Length > 100)
            {
                return new BadRequestObjectResult(new { Message = "Old password must be between 8 and 100 characters.", Code = 5 });
            }

            if (changePasswordRequest.NewPassword.Length < 8 || changePasswordRequest.NewPassword.Length > 100)
            {
                return new BadRequestObjectResult(new { Message = "New password must be between 8 and 100 characters.", Code = 5 });
            }

            // Check if user exists in database
            var refreshToken2 = _httpContextAccessor.HttpContext.Request.Cookies["refreshToken"];
            var userAgent = _httpContextAccessor.HttpContext.Request.Headers["User-Agent"].ToString();

            var user = await FindUserByRefreshToken(refreshToken2, userAgent);

            if (user == null)
            {
                return new BadRequestObjectResult(new { Message = "User not found.", Code = 36 });
            }

            if (!BCrypt.Net.BCrypt.Verify(changePasswordRequest.OldPassword, user.Password))
            {
                return new BadRequestObjectResult(new { Message = "Invalid password.", Code = 37 });
            }

            // Hash the new password
            string newPasswordHashed = BCrypt.Net.BCrypt.HashPassword(changePasswordRequest.NewPassword);

            // Update the user with the new password
            user.Password = newPasswordHashed;
            user.PasswordLastChanged = DateTime.UtcNow;

            var refreshToken = GenerateRefreshToken();

            user.TokenVersion += 1; // Auto invalidates all other tokens

            // Create a new refresh token

            var refreshTokenObj = new RefreshTokens
            {
                UserId = user.Id,
                Token = refreshToken,
                Expires = DateTime.UtcNow.AddDays(7),
                TokenVersion = user.TokenVersion,
                Created = DateTime.UtcNow,
                CreatedByIp = GetUserIp(),
                UserAgent = _httpContextAccessor.HttpContext.Request.Headers["User-Agent"].ToString(),
                DeviceName = "Unknown"
            };

            _dBContext.refreshTokens.Add(refreshTokenObj);
            _dBContext.Users.Update(user);

            await _dBContext.SaveChangesAsync();

            SetCookies(refreshToken);

            // Add to the PasswordChanges table
            var passwordChange = new PasswordChanges
            {
                UserId = user.Id,
                IpAddress = GetUserIp(),
                UserAgent = _httpContextAccessor.HttpContext.Request.Headers["User-Agent"].ToString(),
                Date = DateTime.UtcNow
            };

            _dBContext.passwordChanges.Add(passwordChange);
            await _dBContext.SaveChangesAsync();

            return new OkObjectResult(new { Message = "Password changed successfully!", Code = 78 });
        }

        public async Task<IActionResult> GetUser()
        {

            var httpContex = _httpContextAccessor.HttpContext;

            if (httpContex == null)
            {
                return new UnauthorizedObjectResult(new { Message = "No http context found.", Code = 1000 });
            }

            var refreshToken = httpContex.Request.Cookies["refreshToken"];
            var userAgent = httpContex.Request.Headers["User-Agent"].ToString();

            if (refreshToken == null)
            {
                return new UnauthorizedObjectResult(new { Message = "No refresh token found.", Code = 40 });
            }

            var user = await FindUserByRefreshToken(refreshToken, userAgent);

            if (user == null)
            {
                SetCookies("");
                return new UnauthorizedObjectResult(new { Message = "Invalid refresh token or refresh token has expired.", Code = 41 });
            }


            // Check to see if the user has logged in successfully before on this device

            var ip = GetUserIp();
            var authLog = await _dBContext.authLogs.Where(a => a.IpAddress == ip && a.UserAgent == userAgent && a.Email == user.Email).OrderByDescending(a => a.Date).FirstOrDefaultAsync();

            if (authLog == null)
            {
                SetCookies("");
                return new UnauthorizedObjectResult(new { Message = "No previous login found.", Code = 1000 });
            }


            return new OkObjectResult (new { user.FullName, user.Username, user.Email, user.Role,user.ProfilePicture, user.Id, user.TimeZone, user.Language, user.IsEmailVerified, user.IsTwoFactorEnabled, user.IsExternal });
        }


        public async Task<IActionResult> RefreshToken()
        {
            var httpContex = _httpContextAccessor.HttpContext;


            var refreshToken = httpContex.Request.Cookies["refreshToken"];
            var userAgent = httpContex.Request.Headers["User-Agent"].ToString();
            var currentTime = DateTime.UtcNow;

            if (refreshToken == null)
            {
                return new UnauthorizedObjectResult(new { Message = "No refresh token found.", Code = 40 });
            }


            var user = await FindUserByRefreshToken(refreshToken, httpContex.Request.Headers["User-Agent"].ToString());
            var refreshTokenObj = _dBContext.refreshTokens.FirstOrDefault(r => r.Token == refreshToken && r.Expires > currentTime && r.UserAgent == userAgent && r.Revoked == null);

            if (user == null || refreshTokenObj.IsActive == false)
            {
                return new UnauthorizedObjectResult(new { Message = "Invalid refresh token or refresh token has expired.", Code = 41 });
            }

            // Generate new access token

            string newAccessToken = CreateAccessToken(user);

            return new OkObjectResult(new
            {
                AccessToken = newAccessToken,
                Code = 45,
                Message = "Token refreshed successfully!"
            });
        }


        public string CreateAccessToken(User user)
        {

            List<Claim> claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user?.Username),
                new Claim(ClaimTypes.Email, user?.Email),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("TokenVersion", user.TokenVersion.ToString()),
            };


            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration.GetSection("AppSecurity:Secret").Value!));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(15),
                SigningCredentials = creds
            };


            var jwt = new JwtSecurityTokenHandler();
            var token = jwt.CreateToken(tokenDescriptor);
            var accessToken = jwt.WriteToken(token);


            return accessToken;
        }

        public void SetCookies(string refreshToken)
        {

            var httpContex = _httpContextAccessor.HttpContext;

            // Set cookie for .erzen.tk
            var cookieOptionsTk = new CookieOptions
            {
                HttpOnly = true,
                Expires = DateTime.UtcNow.AddDays(7),
                Secure = true,
                SameSite = SameSiteMode.None,
                Domain = ".erzen.tk"
            };

            // Set cookie for .erzen.xyz
            var cookieOptionsXyz = new CookieOptions
            {
                HttpOnly = true,
                Expires = DateTime.UtcNow.AddDays(7),
                Secure = true,
                SameSite = SameSiteMode.None,
                Domain = ".sapientia.life"
            };

            // Set cookie for localhost
            var cookieOptionsLocal = new CookieOptions
            {
                HttpOnly = true,
                Expires = DateTime.UtcNow.AddDays(7),
                Secure = true,
                SameSite = SameSiteMode.None,
                Domain = "localhost"
            };

            httpContex.Response.Cookies.Append("refreshToken", refreshToken, cookieOptionsTk);
            httpContex.Response.Cookies.Append("refreshToken", refreshToken, cookieOptionsXyz);
            httpContex.Response.Cookies.Append("refreshToken", refreshToken, cookieOptionsLocal);

        }

        public string GetUserIp()
        {
            var httpContex = _httpContextAccessor.HttpContext;
            // Get the value from the X-Forwarded-For header
            var forwardedHeader = httpContex.Request.Headers["X-Forwarded-For"].FirstOrDefault();

            // Check if the header contains multiple IP addresses
            if (!string.IsNullOrEmpty(forwardedHeader))
            {
                // Split the header value by comma to get a list of IP addresses
                var ips = forwardedHeader.Split(',');

                // Return the first IP address which is the client's original IP
                return ips.FirstOrDefault();
            }

            // If there is no X-Forwarded-For header, fall back to the RemoteIpAddress
            return httpContex.Connection.RemoteIpAddress.ToString();
        }

        public string GenerateRefreshToken()
        {
            DateTime dateTime = DateTime.UtcNow;
            var rng = RandomNumberGenerator.Create();
            byte[] randomBytes2 = new byte[64];
            rng.GetBytes(randomBytes2);
            string randomString = Convert.ToBase64String(randomBytes2);
            string dateTimeString = dateTime.ToString("o");
            string tokenString = randomString + dateTimeString;
            string newRefreshToken = Convert.ToBase64String(Encoding.UTF8.GetBytes(tokenString));
            return newRefreshToken;
        }

        public async Task<IActionResult> SetupMfa(string code = "first")
        {
            var refreshToken = _httpContextAccessor.HttpContext.Request.Cookies["refreshToken"];
            var userAgent = _httpContextAccessor.HttpContext.Request.Headers["User-Agent"].ToString();

            var user = await FindUserByRefreshToken(refreshToken, userAgent);

            if (user == null || user.Username == null)
            {
                return new NotFoundObjectResult(new {Message = "User not found", Code = 1000});
            }


            if (!user.IsTwoFactorEnabled && code == "first")
            {
                var mfaService = new MfaService();
                var secret = mfaService.GenerateTotpSecret();

                user.TotpSecret = secret;


                _dBContext.Users.Update(user);
                await _dBContext.SaveChangesAsync();

                var otpauth = mfaService.GenerateQrCodeUri(user.Username, secret);
                var qrCodeImage = mfaService.GenerateQrCodeImage(otpauth);

                // Return as File image qr code
                return new FileContentResult(qrCodeImage, "image/png");

            } else
            {
                // Validate the code and enable MFA

                var secret = user.TotpSecret;

                var totp = new Totp(Base32Encoding.ToBytes(secret));
                var isValid = totp.VerifyTotp(code, out long timeStepMatched, new VerificationWindow(2, 2));

                if (isValid)
                {
                    // Generate backup codes

                    var mfaService = new MfaService();
                    var backupCodes = mfaService.GenerateBackupCodes();

                    user.BackupCodes = backupCodes;
                    user.IsTwoFactorEnabled = true;
                    _dBContext.Users.Update(user);
                    await _dBContext.SaveChangesAsync();

                    return new OkObjectResult(new { Message = "MFA enabled successfully", Codes = backupCodes, Code = 1000 });
                } else
                {
                    return new BadRequestObjectResult(new { Message = "Invalid code", Code = 1000 });
                }

            }

        }

        public async Task<IActionResult> VerifyMfa(string email, string code)
        {

            var user = _dBContext.Users.FirstOrDefault(u => u.Email == email);

            if (user == null || user.Username == null)
            {
                return new NotFoundObjectResult(new { Message = "User not found", Code = 1000 });
            }

            var secret = user.TotpSecret;

            var totp = new Totp(Base32Encoding.ToBytes(secret));
            var isValid = totp.VerifyTotp(code, out long timeStepMatched, new VerificationWindow(2, 2));

            var ip = GetUserIp();
            var userAgent = _httpContextAccessor.HttpContext.Request.Headers["User-Agent"].ToString();

            if (isValid)
            {
                var refreshTokens = _dBContext.refreshTokens.Where(r => r.UserId == user.Id).ToList();

                var currentRefreshTokenVersion = user.TokenVersion;

                string newRefreshToken = GenerateRefreshToken();

                var found = false;

                // Check if any of the refresh tokens are still active

                foreach (var token in refreshTokens)
                {
                    if (currentRefreshTokenVersion == token.TokenVersion && token.IsActive && userAgent == token.UserAgent)
                    {
                        // If the token is active, update the last used time
                        token.LastUsed = DateTime.UtcNow;
                        _dBContext.refreshTokens.Update(token);
                        await _dBContext.SaveChangesAsync();
                        found = true;
                        newRefreshToken = token.Token;
                    }
                }


                if (!found)
                {
                    // If no active refresh token was found, generate a new one
                    var refreshToken = new RefreshTokens
                    {
                        UserId = user.Id,
                        Token = newRefreshToken,
                        Expires = DateTime.UtcNow.AddDays(7),
                        TokenVersion = user.TokenVersion,
                        Created = DateTime.UtcNow,
                        CreatedByIp = ip,
                        UserAgent = userAgent,
                        DeviceName = "Unknown"
                    };

                    _dBContext.refreshTokens.Add(refreshToken);
                    found = true;
                    await _dBContext.SaveChangesAsync();
                }

                SetCookies(newRefreshToken);

                var accessToken = CreateAccessToken(user);

                return new OkObjectResult(new { Message = "MFA verified successfully", Code = 1000, AccessToken = accessToken, newRefreshToken = newRefreshToken });

            }
            else
            {
                // Check to see if the code is a backup code, if so remove from that string example 421iofjafk,3125klhllh etc

                var backupCodes = user.BackupCodes;
                var backupCodesArray = backupCodes.Split(',');

                var foundCode = false;

                foreach (var backupCode in backupCodesArray)
                {
                    if (backupCode == code)
                    {
                        foundCode = true;
                        break;
                    }
                }

                if (!foundCode)
                {
                    return new BadRequestObjectResult(new { Message = "Invalid code", Code = 1000 });
                } 
                else
                {
                    // Remove the code from the backup codes
                    var newBackupCodes = "";

                    foreach (var backupCode in backupCodesArray)
                    {
                        if (backupCode != code)
                        {
                            newBackupCodes += backupCode + ",";
                        }
                    }

                    user.BackupCodes = newBackupCodes;

                    _dBContext.Users.Update(user);
                    await _dBContext.SaveChangesAsync();

                    var refreshTokens = _dBContext.refreshTokens.Where(r => r.UserId == user.Id).ToList();

                    var currentRefreshTokenVersion = user.TokenVersion;

                    string newRefreshToken = GenerateRefreshToken();

                    var found = false;

                    // Check if any of the refresh tokens are still active

                    foreach (var token in refreshTokens)
                    {
                        if (currentRefreshTokenVersion == token.TokenVersion && token.IsActive && userAgent == token.UserAgent)
                        {
                            // If the token is active, update the last used time
                            token.LastUsed = DateTime.UtcNow;
                            _dBContext.refreshTokens.Update(token);
                            await _dBContext.SaveChangesAsync();
                            found = true;
                            newRefreshToken = token.Token;
                        }
                    }


                    if (!found)
                    {
                        // If no active refresh token was found, generate a new one
                        var refreshToken = new RefreshTokens
                        {
                            UserId = user.Id,
                            Token = newRefreshToken,
                            Expires = DateTime.UtcNow.AddDays(7),
                            TokenVersion = user.TokenVersion,
                            Created = DateTime.UtcNow,
                            CreatedByIp = ip,
                            UserAgent = userAgent,
                            DeviceName = "Unknown"
                        };

                        _dBContext.refreshTokens.Add(refreshToken);
                        found = true;
                        await _dBContext.SaveChangesAsync();
                    }

                    SetCookies(newRefreshToken);

                    var accessToken = CreateAccessToken(user);

                    return new OkObjectResult(new { Message = "MFA verified successfully", Code = 1000, AccessToken = accessToken, newRefreshToken = newRefreshToken });
                }
            }

        }

        public async Task<IActionResult> GenerateBackupCodes()
        {
            var refreshToken = _httpContextAccessor.HttpContext.Request.Cookies["refreshToken"];
            var userAgent = _httpContextAccessor.HttpContext.Request.Headers["User-Agent"].ToString();

            var user = await FindUserByRefreshToken(refreshToken, userAgent);

            if (user == null || user.Username == null)
            {
                return new NotFoundObjectResult(new { Message = "User not found", Code = 1000 });
            }

            if (!user.IsTwoFactorEnabled)
            {
                return new BadRequestObjectResult(new { Message = "MFA is not enabled", Code = 1000 });
            }

            var mfaService = new MfaService();
            var backupCodes = mfaService.GenerateBackupCodes();

            user.BackupCodes = backupCodes;

            _dBContext.Users.Update(user);
            await _dBContext.SaveChangesAsync();

            return new OkObjectResult(new { Message = "Backup codes generated successfully", Code = 1000, BackupCodes = backupCodes });
        }

        public async Task<IActionResult> LoginProvider(HttpContext httpContext, string provider)
        {

            if (!provider.Equals("Google") && !provider.Equals("GitHub") && !provider.Equals("Discord"))
            {
                return new BadRequestObjectResult(new { Message = "Invalid provider.", Code = 1000 });
            }

            if (!httpContext.Request.Headers.ContainsKey("X-Forwarded-Proto"))
            {
                httpContext.Request.Headers["X-Forwarded-Proto"] = "https";
            }

            var properties = new AuthenticationProperties { RedirectUri = "/external-login-callback" };
            return new ChallengeResult(provider, properties);
        }

        public async Task<IActionResult> LoginProviderCallback(HttpContext httpContext)
        {
            if (!httpContext.Request.Headers.ContainsKey("X-Forwarded-Proto"))
            {
                httpContext.Request.Headers["X-Forwarded-Proto"] = "https";
            }

            var result = await httpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (!result.Succeeded) return new BadRequestObjectResult(new { Message = "Error processing external login." });

            // Retrieve user info from the external login
            var claims = result.Principal?.Identities.FirstOrDefault()?.Claims;
            var externalUserId = claims?.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;
            var email = claims?.FirstOrDefault(x => x.Type == ClaimTypes.Email)?.Value;

            // Get provider from the external login
            var provider = claims?.FirstOrDefault(x => x.Type == "Provider")?.Value;

            // Check if the user is added in DB

            var user = _dBContext.Users.FirstOrDefault(u => u.ExternalUserId == externalUserId);

            if (user != null)
            {
                // User exists create a refresh token and access token

                var refreshToken = GenerateRefreshToken();

                user.LastLogin = DateTime.UtcNow;

                var currentRefreshTokenVersion = user.TokenVersion;

                // Create a new refresh token

                var refreshTokenObj = new RefreshTokens
                {
                    UserId = user.Id,
                    Token = refreshToken,
                    Expires = DateTime.UtcNow.AddDays(7),
                    TokenVersion = user.TokenVersion,
                    Created = DateTime.UtcNow,
                    CreatedByIp = GetUserIp(),
                    UserAgent = httpContext.Request.Headers["User-Agent"].ToString(),
                    DeviceName = "Unknown"
                };

                await _dBContext.refreshTokens.AddAsync(refreshTokenObj);

                await _dBContext.SaveChangesAsync();

                // Generate new access token

                string newAccessToken = CreateAccessToken(user);

                // Set cookies
                SetCookies(refreshToken);

                // Return the access token
                return new OkObjectResult(new { Message = "User logged in successfully!", Code = 38, AccessToken = newAccessToken, newRefreshToken = refreshToken });
            } else
            {
                // User does not exist, add the user to the database
                var newUser = new User
                {
                    Email = email,
                    Username = email,
                    FullName = claims?.FirstOrDefault(x => x.Type == ClaimTypes.Name)?.Value,
                    TimeZone = "UTC",
                    Language = "en",
                    ProfilePicture = claims?.FirstOrDefault(x => x.Type == "picture")?.Value,
                    Role = "User",
                    IsEmailVerified = true,
                    IsTwoFactorEnabled = false,
                    CreatedAt = DateTime.UtcNow,
                    LastLogin = DateTime.UtcNow,
                    TokenVersion = 1,
                    ConnectingIp = GetUserIp(),
                    Password = BCrypt.Net.BCrypt.HashPassword(""),
                    ExternalProvider = provider,
                    ExternalUserId = externalUserId,
                    IsExternal = true
                };

                await _dBContext.Users.AddAsync(newUser);

                await _dBContext.SaveChangesAsync();

                // User exists create a refresh token and access token

                var refreshToken = GenerateRefreshToken();

                var currentRefreshTokenVersion = user.TokenVersion;

                // Create a new refresh token

                var refreshTokenObj = new RefreshTokens
                {
                    UserId = user.Id,
                    Token = refreshToken,
                    Expires = DateTime.UtcNow.AddDays(7),
                    TokenVersion = user.TokenVersion,
                    Created = DateTime.UtcNow,
                    CreatedByIp = GetUserIp(),
                    UserAgent = httpContext.Request.Headers["User-Agent"].ToString(),
                    DeviceName = "Unknown"
                };

                await _dBContext.refreshTokens.AddAsync(refreshTokenObj);

                await _dBContext.SaveChangesAsync();

                // Generate new access token

                string newAccessToken = CreateAccessToken(user);

                // Set cookies
                SetCookies(refreshToken);

                // Return the access token
                return new OkObjectResult(new { Message = "User logged in successfully!", Code = 38, AccessToken = newAccessToken, newRefreshToken = refreshToken });
            }


        }

        // Find the user by giving a refresh token

        public async Task<User?> FindUserByRefreshToken(string refreshToken, string userAgent)
        {
            var currentTime = DateTime.UtcNow;

            var refreshTokenEntry = _dBContext.refreshTokens.FirstOrDefault(r => r.Token == refreshToken && r.Expires > currentTime && r.UserAgent == userAgent && r.Revoked == null);

            if (refreshTokenEntry == null)
            {
                return null;
            }

            var userId = refreshTokenEntry.UserId;
            var refreshTokenVersion = refreshTokenEntry.TokenVersion;

            var user = await _dBContext.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return null;
            }

            if (user.TokenVersion != refreshTokenVersion)
            {
                return null;
            }

            return user;
        }

    }
}
