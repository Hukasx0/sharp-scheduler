using Microsoft.EntityFrameworkCore;
using Quartz;
using sharp_scheduler.Server.Data;
using sharp_scheduler.Server.Models;
using System.Threading.Tasks;

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

            var activeTasks = await dbContext.ScheduledJobs.Where(t => t.IsActive).ToListAsync();
            foreach (var  task in activeTasks)
            {
                await ScheduleJob(scheduler, task);
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
        }

        public async Task ScheduleJob(IScheduler scheduler, ScheduledJob scheduledJob)
        {
            var job = JobBuilder.Create<JobExecutionService>()
                .WithIdentity($"task-{scheduledJob.Id}")
                .UsingJobData("taskId", scheduledJob.Id)
                .Build();
            
            var trigger = TriggerBuilder.Create()
                .WithIdentity($"trigger-{scheduledJob.Id}")
                .WithCronSchedule(scheduledJob.CronExpression)
                .Build();

            await scheduler.ScheduleJob(job, trigger);
        }
    }
}
