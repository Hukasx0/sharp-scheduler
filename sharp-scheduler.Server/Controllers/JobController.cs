using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quartz;
using sharp_scheduler.Server.Data;
using sharp_scheduler.Server.Models;
using sharp_scheduler.Server.Services;
using System.Threading.Tasks;

namespace sharp_scheduler.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class JobController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ISchedulerFactory _schedulerFactory;

        public JobController(AppDbContext context, ISchedulerFactory schedulerFactory)
        {
            _context = context;
            _schedulerFactory = schedulerFactory;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var tasks = await _context.ScheduledJobs.ToListAsync();
            return Ok(tasks);
        }

        [HttpPost]
        public async Task<IActionResult> Create(ScheduledJob task)
        {
            task.CreatedAt = DateTime.UtcNow;
            task.IsActive = true;

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    await _context.ScheduledJobs.AddAsync(task);
                    await _context.SaveChangesAsync();

                    await ScheduleJob(task);

                    await transaction.CommitAsync();

                    return CreatedAtAction(nameof(GetAll), new { id = task.Id }, task);
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, ScheduledJob scheduledJob)
        {
            if (id != scheduledJob.Id) return BadRequest();

            var existingTask = await _context.ScheduledJobs.FindAsync(id);
            if (existingTask != null) return NotFound();

            existingTask.Name = scheduledJob.Name;
            existingTask.Command = scheduledJob.Command;
            existingTask.CronExpression = scheduledJob.CronExpression;
            existingTask.IsActive = scheduledJob.IsActive;

            await _context.SaveChangesAsync();
            await UpdateJobSchedule(existingTask);

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var task = await _context.ScheduledJobs.FindAsync(id);
            if (task == null) return NotFound();

            _context.ScheduledJobs.Remove(task);
            await _context.SaveChangesAsync();

            var scheduler = await _schedulerFactory.GetScheduler();
            await scheduler.DeleteJob(new JobKey($"task-{task.Id}"));

            return NoContent();
        }

        private async Task ScheduleJob(ScheduledJob task)
        {
            var scheduler = await _schedulerFactory.GetScheduler();
            
            var job = JobBuilder.Create<JobExecutionService>()
                .WithIdentity($"task-{task.Id}")
                .UsingJobData("taskId", task.Id)
                .Build();

            var trigger = TriggerBuilder.Create()
                .WithIdentity($"trigger-{task.Id}")
                .WithCronSchedule(task.CronExpression)
                .Build();

            await scheduler.ScheduleJob(job, trigger);
        }

        private async Task UpdateJobSchedule(ScheduledJob task)
        {
            var scheduler = await _schedulerFactory.GetScheduler();
            await scheduler.DeleteJob(new JobKey($"task-{task.Id}"));

            if (task.IsActive)
            {
                await ScheduleJob(task);
            }
        }
    }
}
