using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using sharp_scheduler.Server.Controllers;
using sharp_scheduler.Server.Data;

namespace sharp_scheduler.Server.Services
{
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

        public async Task<bool> IsLoginAttemptAllowed(string username, string ipAddress)
        {
            IConfigurationSection? bruteforceSettings = _configuration.GetSection("AntiBruteForce");

            if (bruteforceSettings == null || !bruteforceSettings.Exists())
            {
                _logger.LogWarning("AntiBruteForce settings are missing or invalid (in appsettings.json), using default settings");
            }

            int maxUsernameFailedAttempts = bruteforceSettings?.GetValue("MaxUsernameFailedAttempts", 5) ?? 5;
            int maxIpFailedAttempts = bruteforceSettings?.GetValue("MaxIpFailedAttempts", 10) ?? 10;

            var recentFailedAttempts = await _context.LoginLogs
                .Where(l => l.Username == username && l.Status == "Failure"
                            && l.Timestamp > DateTime.UtcNow.AddHours(-1))
                .CountAsync();

            if (recentFailedAttempts >= maxUsernameFailedAttempts)
            {
                return false;
            }

            if (!_memoryCache.TryGetValue(ipAddress, out int ipAttempts)) 
            {
                ipAttempts = 0;
            }

            if (ipAttempts >= maxIpFailedAttempts)
            {
                return false;
            }

            return true;
        }

        public void IncrementIpAttempts(string ipAddress)
        {
            _memoryCache.TryGetValue(ipAddress, out int ipAttempts);

            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromHours(1));

            _memoryCache.Set(ipAddress, ipAttempts+1, cacheEntryOptions);
        }

        public void ResetIpAttempts(string ipAddress)
        {
            _memoryCache.Remove(ipAddress);
        }
    }

    public static class AntiBruteForceExtensions
    {
        public static IServiceCollection AddBruteForceProtection(this IServiceCollection services)
        {
            services.AddMemoryCache();
            services.AddScoped<LoginBruteforceProtectionService>();
            return services;
        }
    }
}
