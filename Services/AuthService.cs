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

namespace NewsAggregation.Services
{
    public class AuthService : IAuthService
    {

        private readonly DBContext _dBContext;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;
        private readonly SecureMail _secureMail;

        public AuthService(DBContext dbContext, IHttpContextAccessor httpContextAccessor, IConfiguration configuration,SecureMail secureMail)
        {
            _dBContext = dbContext;
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
            _secureMail = secureMail;
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

            if (_dBContext.Users.Any(u => u.Email == userRequest.Email))
            {
                return new BadRequestObjectResult(new { Message = "Email already in use.", Code = 7 });
            }

            if (userRequest.Birthdate == null)
            {
                return new BadRequestObjectResult(new { Message = "Birthdate is required.", Code = 6 });
            }

            if (_dBContext.Users.Any(u => u.Username == userRequest.Username))
            {
                return new BadRequestObjectResult(new { Message = "Username already in use.", Code = 8 });
            }

            string passwordHashed = BCrypt.Net.BCrypt.HashPassword(userRequest.Password);
            string email = userRequest.Email;

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
            user.Email = email;
            user.Password = passwordHashed;
            user.FirstLogin = DateTime.UtcNow;
            user.LastLogin = DateTime.UtcNow;
            user.ConnectingIp = GetUserIp();
            user.Birthdate = userRequest.Birthdate;
            user.Role = "User";
            user.Username = userRequest.Username;
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
            var user = _dBContext.Users.FirstOrDefault(u => u.Email == userRequest.Email);
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
                // Add failed login attempt to the AuthLogs table

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
                    } 
                }

                // Generate new refresh token

                string newRefreshToken = GenerateRefreshToken();


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


                SetCookies(newRefreshToken);

                if (oldConIP != GetUserIp())
                {
                    await _secureMail.SendEmail("noreply@erzen.tk", user.Email, "New Login from New IP on Personal Podcast", @"<p style=""font-size: 16px; color: #FF0000; font-weight: bold;"">⚠️ WARNING: SECURITY ALERT!</p>
<p style=""font-size: 14px;"">Dear User,</p>
<p style=""font-size: 14px;"">We regret to inform you that your account has been accessed from a <span style=""color: #FF0000;"">new, unauthorized IP address</span>. This may indicate a <span style=""color: #FF0000;"">security breach</span>.</p>
<p style=""font-size: 14px;"">If this login was not authorized by you, we urge you to <span style=""color: #FF0000;"">immediately change your password</span> by visiting <a href=""https://personalpodcast.erzen.tk/account"">this link</a>.</p>
<p style=""font-size: 14px;"">For your safety, do not ignore this message. If you believe your account has been compromised, <span style=""color: #FF0000;"">contact our support team</span> immediately.</p>
<p style=""font-size: 14px;"">Thank you for your attention to this urgent matter.</p>
<p style=""font-size: 14px;"">Sincerely,</p>
<p style=""font-size: 14px;"">PersonalPodcasts</p>
");
                }


                return new OkObjectResult(new { Message = "User logged in successfully!", Code = 38, AccessToken = accessToken, newRefreshToken });
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

            var user = FindUserByRefreshToken(refreshToken, httpContex.Request.Headers["User-Agent"].ToString());
            var userAgent = httpContex.Request.Headers["User-Agent"].ToString();

            if (user == null)
            {
                return new UnauthorizedObjectResult(new { Message = "Invalid refresh token or refresh token has expired.", Code = 41 });
            }

            // Revoke the refresh token
            var refreshTokenOBJ = _dBContext.refreshTokens.FirstOrDefault(r => r.Token == refreshToken && r.IsActive);

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

        public async Task<IActionResult> ForgotPassword(UserRequest userRequest, string? emailRq, string? code, int? verifyRequest)
        {

            if (verifyRequest == 777)
            {
                var resetEmail = _dBContext.resetEmails.FirstOrDefault(r => r.Email == emailRq && r.Code == code.ToString());
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

                var user2 = _dBContext.Users.FirstOrDefault(u => u.Email == emailRq);

                if (user2 == null)
                {
                    return new BadRequestObjectResult(new { Message = "User was not found.", Code = 36 });
                }

                user2.Password = newPasswordHashed;

                _dBContext.Users.Update(user2);
                await _dBContext.SaveChangesAsync();

                await _secureMail.SendEmail("njnana2017@gmail.com", emailRq, "New Password", "<h1>Hello!</h1><br>You have requested to reset your password in PersonalPodcast.<br>Here is your new password: <strong>" + newPassword + "</strong><br>Thanks!");


                return new OkObjectResult(new { Message = $"Here is your new generated password: {newPassword}, It was also send via email.", Code = 69 });

            }
            else
            {

                if (userRequest.Email == null)
                {
                    return new BadRequestObjectResult(new { Message = "Email is required.", Code = 1 });
                }

                if (userRequest.Email.Length < 5 || userRequest.Email.Length > 100)
                {
                    return new BadRequestObjectResult(new { Message = "Email must be between 5 and 100 characters.", Code = 3 });
                }

                // Check if user exists in database
                var user = _dBContext.Users.FirstOrDefault(u => u.Email == userRequest.Email);
                if (user == null)
                {
                    return new BadRequestObjectResult(new { Message = "User not found.", Code = 36 });
                }

                // Generate new code
                string randomCode = Guid.NewGuid().ToString().Substring(0, 8);


                var emailResetOp = new ResetEmail
                {
                    Email = userRequest.Email,
                    Code = randomCode,
                    CreatedDate = DateTime.UtcNow,
                    ValidUntil = DateTime.UtcNow.AddMinutes(15)

                };

                // Save to DB
                _dBContext.resetEmails.Add(emailResetOp);
                await _dBContext.SaveChangesAsync();

                // Send email with new password

                await _secureMail.SendEmail("njnana2017@gmail.com", user.Email, "Request to Reset Password", $"<h3>Hello {user.FullName}!</h3><br>You have requested to reset your password in PersonalPodcast.<br>Here is your one time reset code: <strong>" + randomCode + "</strong><br>" +
                    "<p>You can use this link to directly change your password</p> " +
                    $"<a href='https://api.personalpodcasts.erzen.tk/forgot-password?emailRq={user.Email}&code={randomCode}&verifyRequest=777'>Reset password</a>" +
                    "Your link will expire in 15 minutes. If you did not request this, please ignore this email.<br>" +
                    "Thanks!");


                return new BadRequestObjectResult(new { Message = "A code was send to your email. ", Code = 68 });
            }

        }

        public async Task<IActionResult> ChangePassword(ChangePasswordRequest changePasswordRequest)
        {
            if (changePasswordRequest.Email == null || changePasswordRequest.OldPassword == null || changePasswordRequest.NewPassword == null)
            {
                return new BadRequestObjectResult(new { Message = "Email, old password and new password are required.", Code = 203 });
            }

            if (changePasswordRequest.Email.Length < 5 || changePasswordRequest.Email.Length > 100)
            {
                return new BadRequestObjectResult(new { Message = "Email must be between 5 and 100 characters.", Code = 4 });
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
            var user = _dBContext.Users.FirstOrDefault(u => u.Email == changePasswordRequest.Email);
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

            if (refreshToken == null)
            {
                return new UnauthorizedObjectResult(new { Message = "No refresh token found.", Code = 40 });
            }

            var user = FindUserByRefreshToken(refreshToken, httpContex.Request.Headers["User-Agent"].ToString());

            // Check to see if the user has logged in successfully before on this device
            
            var ip = GetUserIp();
            var userAgent = httpContex.Request.Headers["User-Agent"].ToString();
            var authLog = _dBContext.authLogs.Where(a => a.IpAddress == ip && a.UserAgent == userAgent && a.Email == user.Email).OrderByDescending(a => a.Date).FirstOrDefault();

            if (authLog == null)
            {
                SetCookies("");
                return new UnauthorizedObjectResult(new { Message = "No previous login found.", Code = 1000 });
            }

            if (user == null)
            {
                return new UnauthorizedObjectResult(new { Message = "Invalid refresh token or refresh token has expired.", Code = 41 });
            }

            return new OkObjectResult (new { user.FullName, user.Username, user.Email, user.Role, user.Id, user.TimeZone, user.Language, user.IsEmailVerified, user.IsTwoFactorEnabled });
        }


        public async Task<IActionResult> RefreshToken()
        {
            var httpContex = _httpContextAccessor.HttpContext;


            var refreshToken = httpContex.Request.Cookies["refreshToken"];

            if (refreshToken == null)
            {
                return new UnauthorizedObjectResult(new { Message = "No refresh token found.", Code = 40 });
            }


            var user = FindUserByRefreshToken(refreshToken, httpContex.Request.Headers["User-Agent"].ToString());
            var refreshTokenObj = _dBContext.refreshTokens.FirstOrDefault(r => r.Token == refreshToken && r.IsActive);

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
            httpContex.Response.Cookies.Append("refreshToken", refreshToken, cookieOptionsTk);

            // Set cookie for .erzen.xyz
            var cookieOptionsXyz = new CookieOptions
            {
                HttpOnly = true,
                Expires = DateTime.UtcNow.AddDays(7),
                Secure = true,
                SameSite = SameSiteMode.None,
                Domain = ".erzen.xyz"
            };
            httpContex.Response.Cookies.Append("refreshToken", refreshToken, cookieOptionsXyz);
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

        // Find the user by giving a refresh token
        public User FindUserByRefreshToken(string refreshToken, string userAgent)
        {
            var user = _dBContext.refreshTokens.FirstOrDefault(r => r.Token == refreshToken && r.IsActive && r.UserAgent == userAgent);

            if (user == null)
            {
                return null;
            }

            return _dBContext.Users.FirstOrDefault(u => u.Id == user.UserId);

        }

    }
}
