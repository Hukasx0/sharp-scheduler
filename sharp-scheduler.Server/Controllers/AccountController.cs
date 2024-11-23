using BCrypt.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Quartz;
using sharp_scheduler.Server.Data;
using sharp_scheduler.Server.DTOs;
using sharp_scheduler.Server.Models;
using sharp_scheduler.Server.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace sharp_scheduler.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AccountController> _logger;
        private readonly LoginBruteforceProtectionService _bruteForceProtectionService;

        public AccountController(AppDbContext context, IConfiguration configuration, ILogger<AccountController> logger, LoginBruteforceProtectionService bruteforceProtectionService)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
            _bruteForceProtectionService = bruteforceProtectionService;
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 1 : pageSize;
            pageSize = pageSize > 50 ? 50 : pageSize;

            var usersQuery = _context.Accounts.AsQueryable();

            var totalUsers = await usersQuery.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalUsers / pageSize);

            var users = await usersQuery
                .OrderBy(u => u.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new AccountDTO
                {
                    Id = u.Id,
                    Username = u.Username
                })
                .ToListAsync();

            var result = new
            {
                TotalUsers = totalUsers,
                TotalPages = totalPages,
                CurrentPage = page,
                Users = users
            };

            return Ok(result);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDTO loginDto)
        {
            string ipAddress = Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            
            if (!await _bruteForceProtectionService.IsLoginAttemptAllowed(loginDto.Username, ipAddress))
            {
                _logger.LogWarning($"Warning! Potential bruteforce attack from IP \"{ipAddress}\" (for username \"{loginDto.Username}\")");
                return StatusCode(StatusCodes.Status429TooManyRequests, "Too many login attempts. Please try again later");
            }
            
            var user = await _context.Accounts
                .FirstOrDefaultAsync(u => u.Username == loginDto.Username);

            if (user == null)
            {
                _bruteForceProtectionService.IncrementIpAttempts(ipAddress);
                await LogLoginAttempt(loginDto.Username, "Failure");
                return Unauthorized("Invalid username or password.");
            }

            if (!BCrypt.Net.BCrypt.Verify(loginDto.Password, user.HashedPassword))
            {
                _bruteForceProtectionService.IncrementIpAttempts(ipAddress);
                await LogLoginAttempt(loginDto.Username, "Failure");
                return Unauthorized("Invalid username or password.");
            }

            _bruteForceProtectionService.ResetIpAttempts(ipAddress);

            var token = GenerateJwtToken(user);
            await LogLoginAttempt(loginDto.Username, "Success");

            return Ok(new { Token = token });
        }

        [HttpPut("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword(ChangePasswordDTO changePasswordDto)
        {
            var username = User.Identity?.Name;

            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized("User identity is missing.");
            }

            var user = await _context.Accounts
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null)
            {
                return Unauthorized("User not found.");
            }

            if (!BCrypt.Net.BCrypt.Verify(changePasswordDto.OldPassword, user.HashedPassword))
            {
                return BadRequest("Old password is incorrect.");
            }

            if (changePasswordDto.NewPassword != changePasswordDto.ConfirmNewPassword)
            {
                return BadRequest("New password and confirmation do not match.");
            }

            if (string.IsNullOrEmpty(changePasswordDto.NewPassword) || changePasswordDto.NewPassword.Length < 6)
            {
                return BadRequest("New password must be at least 6 characters long.");
            }

            user.HashedPassword = BCrypt.Net.BCrypt.HashPassword(changePasswordDto.NewPassword);
            _context.Entry(user).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("delete-account")]
        [Authorize]
        public async Task<IActionResult> DeleteAccount(AccountPasswordDTO deleteAccountDto)
        {
            var username = User.Identity?.Name;

            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized("User identity is missing.");
            }

            var user = await _context.Accounts
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null)
            {
                return Unauthorized("User not found.");
            }

            if (!BCrypt.Net.BCrypt.Verify(deleteAccountDto.Password, user.HashedPassword))
            {
                return BadRequest("Incorrect password.");
            }

            _context.Accounts.Remove(user);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPost("create-new-account")]
        [Authorize]
        public async Task<IActionResult> CreateAccount(CreateNewAccountDTO createAccountDto)
        {
            var existingUser = await _context.Accounts
                .FirstOrDefaultAsync(u => u.Username == createAccountDto.Username);

            if (existingUser != null)
            {
                return BadRequest("User with this username already exists.");
            }

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(createAccountDto.Password);

            var newAccount = new Account
            {
                Username = createAccountDto.Username,
                HashedPassword = hashedPassword
            };

            _context.Accounts.Add(newAccount);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetUsers), new { id = newAccount.Id }, newAccount);
        }

        [HttpDelete("delete-all-accounts")]
        [Authorize]
        public async Task<IActionResult> DeleteAll([FromBody] AccountPasswordDTO verifyPasswordDto)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUser = await _context.Accounts.FindAsync(int.Parse(currentUserId));

            if (currentUser == null)
            {
                return Unauthorized("User not found.");
            }

            if (!BCrypt.Net.BCrypt.Verify(verifyPasswordDto.Password, currentUser.HashedPassword))
            {
                return Unauthorized("Invalid password.");
            }

            var allUsers = await _context.Accounts.ToListAsync();

            _context.Accounts.RemoveRange(allUsers);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet("login-logs")]
        [Authorize]
        public async Task<IActionResult> GetLoginLogs([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 1 : pageSize;
            pageSize = pageSize > 50 ? 50 : pageSize;

            var logsQuery = _context.LoginLogs.AsQueryable();

            var totalLogs = await logsQuery.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalLogs / pageSize);

            var logs = await logsQuery
                .OrderByDescending(l => l.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var result = new
            {
                TotalLogs = totalLogs,
                TotalPages = totalPages,
                CurrentPage = page,
                Logs = logs
            };

            return Ok(result);
        }

        [HttpDelete("login-logs")]
        public async Task<IActionResult> DeleteLoginLogs(AccountPasswordDTO verifyPasswordDto)
        {
            var username = User.Identity?.Name;

            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized("User identity is missing.");
            }

            var user = await _context.Accounts
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null)
            {
                return Unauthorized("User not found.");
            }

            if (!BCrypt.Net.BCrypt.Verify(verifyPasswordDto.Password, user.HashedPassword))
            {
                return BadRequest("Incorrect password.");
            }

            var logs = await _context.LoginLogs.ToListAsync();

            if (!logs.Any())
            {
                return NotFound("No login logs found to delete.");
            }

            _context.LoginLogs.RemoveRange(logs);
            await _context.SaveChangesAsync();

            return NoContent();
        }




        // Generates a JWT token for a user, containing the user's username as a claim.
        // The token is signed using a symmetric key, with an expiration time set to 1 day.
        private string GenerateJwtToken(Account user)
        {
            var jwtKey = _configuration["Jwt:Key"];
            var jwtIssuer = _configuration["Jwt:Issuer"];
            var jwtAudience = _configuration["Jwt:Audience"];

            // Check if the JWT configuration is properly set in the appsettings or environment variables.
            if (string.IsNullOrEmpty(jwtKey) || string.IsNullOrEmpty(jwtIssuer) || string.IsNullOrEmpty(jwtAudience))
            {
                _logger.LogError("JWT configuration is incomplete. Please check your appsettings.json or environment variables.");
                throw new InvalidOperationException("JWT configuration is incomplete.");
            }

            // Define the claims for the JWT token, which includes the username of the user.
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, user.Username)
            };

            // Create a symmetric key from the JWT secret key and define signing credentials
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Create the JWT token with the necessary claims and expiration settings.
            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: DateTime.Now.AddDays(1),
                signingCredentials: creds
            );

            // Return the generated token as a string
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // Logs a login attempt in the database, storing the username, status, and IP address.
        // The method ensures that only the most recent 1000 login attempts are kept in the database.
        private async Task LogLoginAttempt(string username, string status)
        {
            var ipAddress = Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

            // Create a new login log entry with the given details.
            var loginLog = new LoginLog
            {
                Username = username,
                Status = status,
                Timestamp = DateTime.UtcNow,
                IpAddress = ipAddress
            };

            // Add the new log entry to the database and save the changes.
            _context.LoginLogs.Add(loginLog);
            await _context.SaveChangesAsync();

            // Check the total number of login logs and remove the oldest log if the count exceeds 1000.
            var logCount = await _context.LoginLogs.CountAsync();
            if (logCount > 1000)
            {
                var oldestLog = await _context.LoginLogs
                    .OrderBy(l => l.Timestamp)
                    .FirstOrDefaultAsync();

                if (oldestLog != null)
                {
                    // Remove the oldest log entry from the database to maintain the log count.
                    _context.LoginLogs.Remove(oldestLog);
                    await _context.SaveChangesAsync();
                }
            }
        }
    }
}
