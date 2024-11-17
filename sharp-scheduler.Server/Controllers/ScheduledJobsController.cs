using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quartz;
using sharp_scheduler.Server.Data;
using sharp_scheduler.Server.DTOs;
using sharp_scheduler.Server.Models;
using sharp_scheduler.Server.Services;
using System.Text.RegularExpressions;

namespace sharp_scheduler.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ScheduledJobsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ISchedulerFactory _schedulerFactory;

        public ScheduledJobsController(AppDbContext context, ISchedulerFactory schedulerFactory)
        {
            _context = context;
            _schedulerFactory = schedulerFactory;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var jobs = await _context.ScheduledJobs.ToListAsync();
            return Ok(jobs);
        }

        // cron expression in Quartz .NET format
        [HttpPost]
        public async Task<IActionResult> Create(ScheduledJobPostDTO newJob)
        {
            var job = new ScheduledJob
            {
                Name = newJob.Name,
                Command = newJob.Command,
                CronExpression = newJob.CronExpression,
                CreatedAt = DateTime.UtcNow,
                LastResult = ""
            };

            using (var transaction = await  _context.Database.BeginTransactionAsync())
            {
                try
                {
                    await _context.ScheduledJobs.AddAsync(job);
                    await _context.SaveChangesAsync();

                    await ScheduleJob(job);

                    await transaction.CommitAsync();

                    return CreatedAtAction(nameof(GetAll), new { id = job.Id }, job);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    Console.WriteLine(ex.ToString());
                    throw;
                }
            }
        }

        // cron expression in Quartz .NET format
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, ScheduledJobPostDTO updateJob)
        {
            var existingJob = await _context.ScheduledJobs.FindAsync(id);
            if (existingJob == null) return NotFound();

            existingJob.Name = updateJob.Name;
            existingJob.Command = updateJob.Command;
            existingJob.CronExpression = updateJob.CronExpression;

            _context.Entry(existingJob).State = EntityState.Modified;

            await _context.SaveChangesAsync();

            // update the schedule
            var scheduler = await _schedulerFactory.GetScheduler();
            await scheduler.DeleteJob(new JobKey($"job-{existingJob.Id}"));

            await ScheduleJob(existingJob);

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var job = await _context.ScheduledJobs.FindAsync(id);
            if (job == null) return NotFound();

            _context.ScheduledJobs.Remove(job);
            await _context.SaveChangesAsync();

            var scheduler = await _schedulerFactory.GetScheduler();
            await scheduler.DeleteJob(new JobKey($"job-{job.Id}"));

            return NoContent();
        }

        // import jobs from a cron file
        // import from Unix/Linux format
       /* [HttpPost("import")]
        public async Task<IActionResult> ImportCronJobs(IFormFile cronFile)
        {  
        }*/

        // export all jobs in a cron file
        // export in Unix/Linux format
        [HttpGet("export")]
        /*public async Task<IActionResult> ExportCronJobs()
        {
        }*/


        private async Task ScheduleJob(ScheduledJob task)
        {
            var scheduler = await _schedulerFactory.GetScheduler();

            var job = JobBuilder.Create<JobExecutionService>()
                .WithIdentity($"job-{task.Id}")
                .UsingJobData("jobId", task.Id)
                .Build();

            var trigger = TriggerBuilder.Create()
                .WithIdentity($"trigger-{task.Id}")
                .WithCronSchedule(task.CronExpression)
                .Build();

            await scheduler.ScheduleJob(job, trigger);
        }
    }
}
