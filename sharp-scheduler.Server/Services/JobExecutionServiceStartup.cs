using Microsoft.EntityFrameworkCore;
using Quartz;
using sharp_scheduler.Server.Data;
using sharp_scheduler.Server.Models;

namespace sharp_scheduler.Server.Services
{
    public class JobExecutionServiceStartup : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ISchedulerFactory _schedulerFactory;

        public JobExecutionServiceStartup(IServiceProvider serviceProvider, ISchedulerFactory schedulerFactory)
        {
            _serviceProvider = serviceProvider;
            _schedulerFactory = schedulerFactory;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();

            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);

            var jobs = await dbContext.ScheduledJobs.ToListAsync();
            foreach (var job in jobs)
            {
                await ScheduleJob(scheduler, job);
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
        }

        public async Task ScheduleJob(IScheduler scheduler, ScheduledJob scheduledJob)
        {
            var job = JobBuilder.Create<JobExecutionService>()
                .WithIdentity($"job-{scheduledJob.Id}")
                .UsingJobData("jobId", scheduledJob.Id)
                .Build();

            var trigger = TriggerBuilder.Create()
                .WithIdentity($"trigger-{scheduledJob.Id}")
                .WithCronSchedule(scheduledJob.CronExpression)
                .Build();

            await scheduler.ScheduleJob(job, trigger);
        }
    }
}
