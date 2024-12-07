using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Quartz;
using sharp_scheduler.Server.Models;
using sharp_scheduler.Server.Services;
using sharp_scheduler.Server.Data;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace sharp_scheduler.Server.Test
{
    public class LoginBruteforceProtectionServiceTests
    {
        private readonly Mock<AppDbContext> _mockDbContext;
        private readonly Mock<IMemoryCache> _mockMemoryCache;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<ILogger<LoginBruteforceProtectionService>> _mockLogger;
        private readonly LoginBruteforceProtectionService _service;

        public LoginBruteforceProtectionServiceTests()
        {
            _mockDbContext = new Mock<AppDbContext>();
            _mockMemoryCache = new Mock<IMemoryCache>();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockLogger = new Mock<ILogger<LoginBruteforceProtectionService>>();
            _service = new LoginBruteforceProtectionService(
                _mockDbContext.Object,
                _mockMemoryCache.Object,
                _mockConfiguration.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task IsLoginAttemptAllowed_ShouldReturnFalse_WhenFailedAttemptsExceedLimit()
        {
            // Arrange
            var username = "user1";
            var ipAddress = "192.168.1.1";
            _mockConfiguration.Setup(c => c.GetSection("AntiBruteForce")).Returns(Mock.Of<IConfigurationSection>());

            var loginLogs = new List<LoginLog>
            {
                new LoginLog { Username = username, Status = "Failure", Timestamp = DateTime.UtcNow.AddMinutes(-30) },
                new LoginLog { Username = username, Status = "Failure", Timestamp = DateTime.UtcNow.AddMinutes(-30) }
            };

            var dbSetMock = new Mock<DbSet<LoginLog>>();
            dbSetMock.As<IQueryable<LoginLog>>()
                .Setup(m => m.Provider).Returns(loginLogs.AsQueryable().Provider);
            dbSetMock.As<IQueryable<LoginLog>>()
                .Setup(m => m.Expression).Returns(loginLogs.AsQueryable().Expression);
            dbSetMock.As<IQueryable<LoginLog>>()
                .Setup(m => m.ElementType).Returns(loginLogs.AsQueryable().ElementType);
            dbSetMock.As<IQueryable<LoginLog>>()
                .Setup(m => m.GetEnumerator()).Returns(loginLogs.GetEnumerator());

            _mockDbContext.Setup(db => db.LoginLogs).Returns(dbSetMock.Object);

            // Act
            var result = await _service.IsLoginAttemptAllowed(username, ipAddress);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IncrementIpAttempts_ShouldIncreaseAttemptsInMemoryCache()
        {
            // Arrange
            var ipAddress = "192.168.1.1";

            // Act
            _service.IncrementIpAttempts(ipAddress);

            // Assert
            _mockMemoryCache.Verify(m => m.Set(ipAddress, It.IsAny<int>(), It.IsAny<MemoryCacheEntryOptions>()), Times.Once);
        }

        [Fact]
        public void ResetIpAttempts_ShouldRemoveAttemptsFromMemoryCache()
        {
            // Arrange
            var ipAddress = "192.168.1.1";

            // Act
            _service.ResetIpAttempts(ipAddress);

            // Assert
            _mockMemoryCache.Verify(m => m.Remove(ipAddress), Times.Once);
        }
    }
}
