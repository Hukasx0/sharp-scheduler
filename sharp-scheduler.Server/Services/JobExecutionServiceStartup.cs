using Microsoft.EntityFrameworkCore;
using Quartz;
using sharp_scheduler.Server.Data;
using sharp_scheduler.Server.Models;

namespace sharp_scheduler.Server.Services
{
    // Manages the startup and scheduling of jobs based on the configuration from the database.
    // Schedules jobs upon application startup, ensuring active jobs are properly executed.
    public class JobExecutionServiceStartup : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ISchedulerFactory _schedulerFactory;

        public JobExecutionServiceStartup(IServiceProvider serviceProvider, ISchedulerFactory schedulerFactory)
        {
            _serviceProvider = serviceProvider;
            _schedulerFactory = schedulerFactory;
        }

        // Called at application startup to schedule active jobs from the database.
        // Retrieves the scheduled jobs and sets them up with Quartz Scheduler.
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();

            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);

            // Retrieve all active jobs from the database
            var jobs = await dbContext.ScheduledJobs.Where(j => j.IsActive).ToListAsync();
            foreach (var job in jobs)
            {
                // Schedule each job with its associated cron expression
                await ScheduleJob(scheduler, job);
            }
        }

        // StopAsync does not perform any specific cleanup here
        public async Task StopAsync(CancellationToken cancellationToken)
        {
        }

        // Schedules a job with Quartz, specifying the job's identity and trigger settings.
        public async Task ScheduleJob(IScheduler scheduler, ScheduledJob scheduledJob)
        {
            // Create a Quartz job instance, associating it with the job's ID
            var job = JobBuilder.Create<JobExecutionService>()
                .WithIdentity($"job-{scheduledJob.Id}")
                .UsingJobData("jobId", scheduledJob.Id)
                .Build();

            // Create a trigger with a cron expression to control the job's schedule
            var trigger = TriggerBuilder.Create()
                .WithIdentity($"trigger-{scheduledJob.Id}")
                .WithCronSchedule(scheduledJob.CronExpression)
                .Build();

            // Schedule the job with the Quartz scheduler
            await scheduler.ScheduleJob(job, trigger);
        }
    }
}
