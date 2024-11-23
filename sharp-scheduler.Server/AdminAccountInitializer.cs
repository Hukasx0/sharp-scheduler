using BCrypt.Net;
using sharp_scheduler.Server.Data;
using sharp_scheduler.Server.Models;

namespace sharp_scheduler.Server
{
    // This class is responsible for initializing the admin account during application startup.
    // It checks if the admin account already exists, and if not, creates one using the credentials
    // provided in the appsettings.json file under the "AdminAccount" section.
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

        // This method initializes the admin account asynchronously during startup.
        // It checks if the admin account exists in the database, and if not, creates a new one.
        public async Task InitializeAsync()
        {
            // Read the AdminAccount configuration section from the appsettings.json file
            var adminConfig = _configuration.GetSection("AdminAccount");

            // Retrieve the username and password from the configuration
            var username = adminConfig["Username"];
            var password = adminConfig["Password"];

            // If either the username or password is missing, log an error and throw an exception
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                _logger.LogError("Admin account configuration is incomplete. Please provide both 'Username' and 'Password' in the appsettings.json file.");
                throw new InvalidOperationException("Admin account configuration is incomplete.");
            }

            // Check if the admin account already exists in the database by looking for the username
            if (!_context.Accounts.Any(a => a.Username == username))
            {
                // If the account doesn't exist, hash the provided password using BCrypt
                var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

                // Create a new Account object for the admin
                var adminAccount = new Account
                {
                    Username = username,
                    HashedPassword = hashedPassword
                };

                // Add the new admin account to the database and save the changes
                _context.Accounts.Add(adminAccount);
                await _context.SaveChangesAsync();

                // Log a message indicating that the admin account was created successfully
                _logger.LogInformation($"Admin account with username \"{username}\" created successfully!");
            }
            else
            {
                // If the admin account already exists, log that the account creation step was skipped
                _logger.LogInformation("Admin account already exists (skip account creation part)");
            }
        }
    }
}
