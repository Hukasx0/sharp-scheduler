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

        public AccountController(AppDbContext context, IConfiguration configuration, ILogger<AccountController> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _context.Accounts.ToListAsync();
            return Ok(users);
        }
        
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO loginDto)
        {
            var user = await _context.Accounts.FirstOrDefaultAsync(x => x.Username == loginDto.Username);

            if (user == null) return Unauthorized("Invalid username or password");

            if (!BCrypt.Net.BCrypt.Verify(loginDto.Password, user.HashedPassword)) 
            {
                return Unauthorized("Invalid username or password");
            }

            var token = GenerateJwtToken(user);

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
        public async Task<IActionResult> DeleteAccount(DeleteAccountDTO deleteAccountDto)
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
        public async Task<IActionResult> DeleteAll([FromBody] DeleteAccountDTO verifyPasswordDto)
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



        private string GenerateJwtToken(Account user)
        {
            var jwtKey = _configuration["Jwt:Key"];
            var jwtIssuer = _configuration["Jwt:Issuer"];
            var jwtAudience = _configuration["Jwt:Audience"];

            if (string.IsNullOrEmpty(jwtKey) || string.IsNullOrEmpty(jwtIssuer) || string.IsNullOrEmpty(jwtAudience))
            {
                _logger.LogError("JWT configuration is incomplete. Please check your appsettings.json or environment variables.");
                throw new InvalidOperationException("JWT configuration is incomplete.");
            }

            var claims = new[]
            {
        new Claim(ClaimTypes.Name, user.Username)
    };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: DateTime.Now.AddDays(1),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
