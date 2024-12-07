using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using sharp_scheduler.Server.Models;
using sharp_scheduler.Server.Services;
using sharp_scheduler.Server.Data;
using Quartz;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;

namespace sharp_scheduler.Server.Test
{
    public class JobExecutionServiceTests
    {
        private readonly Mock<AppDbContext> _mockDbContext;
        private readonly Mock<DbSet<ScheduledJob>> _mockScheduledJobs;
        private readonly Mock<DbSet<JobExecutionLog>> _mockJobExecutionLogs;
        private readonly JobExecutionService _service;

        public JobExecutionServiceTests()
        {
            // Mock DbContextOptions
            var dbContextOptionsMock = new Mock<DbContextOptions<AppDbContext>>();

            // Mock AppDbContext with DbContextOptions
            _mockDbContext = new Mock<AppDbContext>(dbContextOptionsMock.Object);

            // Mock DbSets for ScheduledJobs and JobExecutionLogs
            _mockScheduledJobs = new Mock<DbSet<ScheduledJob>>();
            _mockJobExecutionLogs = new Mock<DbSet<JobExecutionLog>>();

            // Set up the mock DbContext to return mocked DbSets
            _mockDbContext.Setup(x => x.ScheduledJobs).Returns(_mockScheduledJobs.Object);
            _mockDbContext.Setup(x => x.JobExecutionLogs).Returns(_mockJobExecutionLogs.Object);

            // Create the service instance
            _service = new JobExecutionService(_mockDbContext.Object);
        }

        [Fact]
        public async Task Execute_ShouldLogExecution_WhenJobExists()
        {
            // Arrange
            var jobId = 1;
            var job = new ScheduledJob { Id = jobId, Name = "Test Job", Command = "echo Hello", IsActive = true };
            var contextMock = new Mock<IJobExecutionContext>();
            contextMock.Setup(c => c.JobDetail.JobDataMap.GetInt("jobId")).Returns(jobId);

            _mockScheduledJobs.Setup(x => x.FindAsync(It.IsAny<int>())).ReturnsAsync(job);

            // Act
            await _service.Execute(contextMock.Object);

            // Assert
            _mockDbContext.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            _mockJobExecutionLogs.Verify(logs => logs.Add(It.IsAny<JobExecutionLog>()), Times.Once);
        }

        [Fact]
        public async Task Execute_ShouldNotLogExecution_WhenJobDoesNotExist()
        {
            // Arrange
            var jobId = 1;
            var contextMock = new Mock<IJobExecutionContext>();
            contextMock.Setup(c => c.JobDetail.JobDataMap.GetInt("jobId")).Returns(jobId);

            _mockScheduledJobs.Setup(x => x.FindAsync(It.IsAny<int>())).ReturnsAsync((ScheduledJob)null);

            // Act
            await _service.Execute(contextMock.Object);

            // Assert
            _mockDbContext.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
            _mockJobExecutionLogs.Verify(logs => logs.Add(It.IsAny<JobExecutionLog>()), Times.Never);
        }

        [Fact]
        public async Task Execute_ShouldRollbackTransaction_WhenExceptionOccurs()
        {
            // Arrange
            var jobId = 1;
            var job = new ScheduledJob { Id = jobId, Name = "Test Job", Command = "echo Hello", IsActive = true };
            var contextMock = new Mock<IJobExecutionContext>();
            contextMock.Setup(c => c.JobDetail.JobDataMap.GetInt("jobId")).Returns(jobId);

            _mockScheduledJobs.Setup(x => x.FindAsync(It.IsAny<int>())).ReturnsAsync(job);
            _mockDbContext.Setup(x => x.Database.BeginTransactionAsync(It.IsAny<CancellationToken>())).Throws(new Exception("Test Exception"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _service.Execute(contextMock.Object));
        }
    }
}
