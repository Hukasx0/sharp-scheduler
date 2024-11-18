using BCrypt.Net;
using sharp_scheduler.Server.Data;
using sharp_scheduler.Server.Models;

namespace sharp_scheduler.Server
{
    public class AdminAccountInitializer
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AdminAccountInitializer> _logger;

        public AdminAccountInitializer(AppDbContext context, IConfiguration configuration, ILogger<AdminAccountInitializer> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task InitializeAsync()
        {
            var adminConfig = _configuration.GetSection("AdminAccount");

            var username = adminConfig["Username"];
            var password = adminConfig["Password"];

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                _logger.LogError("Admin account configuration is incomplete. Please provide both 'Username' and 'Password' in the appsettings.json file.");
                throw new InvalidOperationException("Admin account configuration is incomplete.");
            }

            if (!_context.Accounts.Any(a => a.Username == username))
            {
                var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

                var adminAccount = new Account
                {
                    Username = username,
                    HashedPassword = hashedPassword
                };

                _context.Accounts.Add(adminAccount);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Admin account with username \"{username}\" created successfully!");
            }
            else
            {
                _logger.LogInformation("Admin account already exists (skip account creation part)");
            }
        }
    }
}
