using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Quartz;
using sharp_scheduler.Server.Models;
using sharp_scheduler.Server.Services;
using sharp_scheduler.Server.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Microsoft.Extensions.DependencyInjection;

namespace sharp_scheduler.Server.Test
{
    public class JobExecutionServiceStartupTests
    {
        private readonly Mock<IServiceProvider> _mockServiceProvider;
        private readonly Mock<ISchedulerFactory> _mockSchedulerFactory;
        private readonly Mock<IScheduler> _mockScheduler;
        private readonly JobExecutionServiceStartup _startupService;

        public JobExecutionServiceStartupTests()
        {
            _mockServiceProvider = new Mock<IServiceProvider>();
            _mockSchedulerFactory = new Mock<ISchedulerFactory>();
            _mockScheduler = new Mock<IScheduler>();
            _startupService = new JobExecutionServiceStartup(_mockServiceProvider.Object, _mockSchedulerFactory.Object);
        }

        [Fact]
        public async Task StartAsync_ShouldScheduleJobs_WhenJobsExist()
        {
            // Arrange
            var scheduledJobs = new List<ScheduledJob>
            {
                new ScheduledJob { Id = 1, CronExpression = "0/5 * * * * ?", IsActive = true }
            };

            var dbSetMock = new Mock<DbSet<ScheduledJob>>();
            dbSetMock.As<IQueryable<ScheduledJob>>()
                .Setup(m => m.Provider).Returns(scheduledJobs.AsQueryable().Provider);
            dbSetMock.As<IQueryable<ScheduledJob>>()
                .Setup(m => m.Expression).Returns(scheduledJobs.AsQueryable().Expression);
            dbSetMock.As<IQueryable<ScheduledJob>>()
                .Setup(m => m.ElementType).Returns(scheduledJobs.AsQueryable().ElementType);
            dbSetMock.As<IQueryable<ScheduledJob>>()
                .Setup(m => m.GetEnumerator()).Returns(scheduledJobs.GetEnumerator());

            var dbContextMock = new Mock<AppDbContext>();
            dbContextMock.Setup(x => x.ScheduledJobs).Returns(dbSetMock.Object);

            _mockServiceProvider.Setup(x => x.GetRequiredService<AppDbContext>()).Returns(dbContextMock.Object);
            _mockSchedulerFactory.Setup(x => x.GetScheduler(It.IsAny<CancellationToken>())).ReturnsAsync(_mockScheduler.Object);

            // Act
            await _startupService.StartAsync(CancellationToken.None);

            // Assert
            _mockScheduler.Verify(s => s.ScheduleJob(It.IsAny<IJobDetail>(), It.IsAny<ITrigger>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
