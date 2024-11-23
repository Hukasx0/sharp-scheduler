using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using sharp_scheduler.Server.Controllers;
using sharp_scheduler.Server.Data;

namespace sharp_scheduler.Server.Services
{
    // Each login attempt, including the username and IP address, is stored in the database via LoginLog.
    // Failed login attempts for a username are checked from the database, while IP-based attempts are tracked 
    // in memory cache with temporary bans enforced for excessive failed attempts from the same IP address.
    public class LoginBruteforceProtectionService
    {
        private readonly AppDbContext _context;
        private readonly IMemoryCache _memoryCache;
        private readonly IConfiguration _configuration;
        private readonly ILogger<LoginBruteforceProtectionService> _logger;

        public LoginBruteforceProtectionService(AppDbContext context, IMemoryCache memoryCache, IConfiguration configuration, ILogger<LoginBruteforceProtectionService> logger)
        {
            _context = context;
            _memoryCache = memoryCache;
            _configuration = configuration;
            _logger = logger;
        }

        // Determines if the login attempt is allowed based on the number of recent failed login attempts
        // for the given username (from the database) and IP address (from the in-memory cache). 
        // Returns false if the limits are exceeded, otherwise true.
        public async Task<bool> IsLoginAttemptAllowed(string username, string ipAddress)
        {
            IConfigurationSection? bruteforceSettings = _configuration.GetSection("AntiBruteForce");

            if (bruteforceSettings == null || !bruteforceSettings.Exists())
            {
                _logger.LogWarning("AntiBruteForce settings are missing or invalid (in appsettings.json), using default settings");
            }

            int maxUsernameFailedAttempts = bruteforceSettings?.GetValue("MaxUsernameFailedAttempts", 5) ?? 5;
            int maxIpFailedAttempts = bruteforceSettings?.GetValue("MaxIpFailedAttempts", 10) ?? 10;

            // Query the database to count recent failed login attempts for the given username within the last hour
            var recentFailedAttempts = await _context.LoginLogs
                .Where(l => l.Username == username && l.Status == "Failure"
                            && l.Timestamp > DateTime.UtcNow.AddHours(-1))
                .CountAsync();

            // If the number of failed attempts exceeds the limit for the username, deny the attempt
            if (recentFailedAttempts >= maxUsernameFailedAttempts)
            {
                return false;
            }

            // Check the cache for failed attempts from the given IP address
            if (!_memoryCache.TryGetValue(ipAddress, out int ipAttempts))
            {
                ipAttempts = 0;
            }

            // If the number of failed attempts exceeds the limit for the IP address, deny the attempt
            if (ipAttempts >= maxIpFailedAttempts)
            {
                return false;
            }

            return true;
        }

        // Increments the count of failed login attempts for the given IP address. 
        // The attempts are stored in memory with a sliding expiration of 1 hour, meaning that failed attempts 
        // within that time frame will be tracked, but attempts outside of this window will reset the count.
        public void IncrementIpAttempts(string ipAddress)
        {
            // Retrieve current failed attempts count for the IP address from the cache
            _memoryCache.TryGetValue(ipAddress, out int ipAttempts);

            // Define cache expiration policy (sliding expiration of 1 hour)
            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromHours(1));

            // Increment the IP attempts count and update the cache
            _memoryCache.Set(ipAddress, ipAttempts + 1, cacheEntryOptions);
        }

        // Removes the IP address from the cache, effectively resetting the failed login attempts count for that IP.
        public void ResetIpAttempts(string ipAddress)
        {
            _memoryCache.Remove(ipAddress);
        }
    }

    public static class AntiBruteForceExtensions
    {
        public static IServiceCollection AddBruteForceProtection(this IServiceCollection services)
        {
            services.AddMemoryCache(); // Register memory cache service
            services.AddScoped<LoginBruteforceProtectionService>(); // Register the brute-force protection service
            return services;
        }
    }
}
