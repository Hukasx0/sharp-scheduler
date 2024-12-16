using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quartz;
using sharp_scheduler.Server.Data;
using sharp_scheduler.Server.DTOs;
using sharp_scheduler.Server.Models;
using sharp_scheduler.Server.Services;
using System.Text;
using System.Text.RegularExpressions;

namespace sharp_scheduler.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
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
        public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 1 : pageSize;
            pageSize = pageSize > 50 ? 50 : pageSize;

            var jobsQuery = _context.ScheduledJobs.AsQueryable();

            var totalJobs = await jobsQuery.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalJobs / pageSize);

            var jobs = await jobsQuery
                .OrderByDescending(j => j.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var result = new
            {
                TotalJobs = totalJobs,
                TotalPages = totalPages,
                CurrentPage = page,
                Jobs = jobs
            };

            return Ok(result);
        }

        // cron expression in Quartz .NET format
        [HttpPost]
        public async Task<IActionResult> Create(ScheduledJobPostDTO newJob)
        {
            if (string.IsNullOrEmpty(newJob.Name) || string.IsNullOrEmpty(newJob.Command) || string.IsNullOrEmpty(newJob.CronExpression))
            {
                return BadRequest("Name, Command, and CronExpression are required.");
            }

            if (!IsValidCronExpression(newJob.CronExpression))
            {
                return BadRequest("Invalid cron expression format.");
            }

            var job = new ScheduledJob
            {
                Name = newJob.Name,
                Command = newJob.Command,
                CronExpression = newJob.CronExpression,
                CreatedAt = DateTime.UtcNow,
                IsActive = newJob.IsActive
            };

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    await _context.ScheduledJobs.AddAsync(job);
                    await _context.SaveChangesAsync();

                    if (job.IsActive)
                    {
                        await ScheduleJob(job);
                    }

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

        [HttpPut("active-many")]
        public async Task<IActionResult> ActivateJobs([FromBody] List<ActivateManyJobDTO> jobUpdates)
        {
            if (jobUpdates == null || !jobUpdates.Any())
            {
                return BadRequest("No jobs to update.");
            }

            var jobs = await _context.ScheduledJobs
                .Where(j => jobUpdates.Select(u => u.Id).Contains(j.Id))
                .ToListAsync();

            if (jobs.Count != jobUpdates.Count)
            {
                return NotFound("Some jobs were not found.");
            }

            var scheduler = await _schedulerFactory.GetScheduler();

            foreach (var update in jobUpdates)
            {
                var job = jobs.FirstOrDefault(j => j.Id == update.Id);
                if (job == null)
                {
                    continue;
                }

                job.IsActive = update.IsActive;
                _context.Entry(job).State = EntityState.Modified;

                if (update.IsActive)
                {
                    await ScheduleJob(job);
                }
                else
                {
                    var jobKey = new JobKey($"job-{job.Id}");
                    await scheduler.DeleteJob(jobKey);
                }
            }

            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPost("many")]
        public async Task<IActionResult> CreateMany(List<ScheduledJobPostDTO> newJobs)
        {
            if (newJobs == null || newJobs.Count == 0)
            {
                return BadRequest("At least one job must be provided.");
            }

            var invalidJobs = new List<ScheduledJobPostDTO>();
            var jobsToAdd = new List<ScheduledJob>();

            foreach (var newJob in newJobs)
            {
                if (string.IsNullOrEmpty(newJob.Name) || string.IsNullOrEmpty(newJob.Command) || string.IsNullOrEmpty(newJob.CronExpression))
                {
                    invalidJobs.Add(newJob);
                    continue;
                }

                if (!IsValidCronExpression(newJob.CronExpression))
                {
                    invalidJobs.Add(newJob);
                    continue;
                }

                var job = new ScheduledJob
                {
                    Name = newJob.Name,
                    Command = newJob.Command,
                    CronExpression = newJob.CronExpression,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = newJob.IsActive
                };

                jobsToAdd.Add(job);
            }

            if (invalidJobs.Count > 0)
            {
                return BadRequest(new
                {
                    message = "Some jobs are invalid.",
                    invalidJobs = invalidJobs
                });
            }

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    await _context.ScheduledJobs.AddRangeAsync(jobsToAdd);
                    await _context.SaveChangesAsync();

                    foreach (var job in jobsToAdd)
                    {
                        await ScheduleJob(job);
                    }

                    await transaction.CommitAsync();

                    return CreatedAtAction(nameof(GetAll), new { }, jobsToAdd);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    Console.WriteLine(ex.ToString());
                    return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing the jobs.");
                }
            }
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteAll()
        {
            var jobs = await _context.ScheduledJobs.ToListAsync();

            if (jobs.Count == 0)
            {
                return NotFound("No scheduled jobs found to delete.");
            }

            _context.ScheduledJobs.RemoveRange(jobs);
            await _context.SaveChangesAsync();

            var scheduler = await _schedulerFactory.GetScheduler();

            foreach (var job in jobs)
            {
                var jobKey = new JobKey($"job-{job.Id}");
                await scheduler.DeleteJob(jobKey);
            }

            return NoContent();
        }

        // cron expression in Quartz .NET format
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, ScheduledJobPostDTO updateJob)
        {
            var existingJob = await _context.ScheduledJobs.FindAsync(id);
            if (existingJob == null) return NotFound();

            if (!IsValidCronExpression(updateJob.CronExpression))
            {
                return BadRequest("Invalid cron expression format.");
            }

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

        [HttpPut("{id}/active")]
        public async Task<IActionResult> ActivateJob(int id, [FromBody] ActivateJobDTO isActive)
        {
            var job = await _context.ScheduledJobs.FindAsync(id);
            if (job == null) return NotFound();

            job.IsActive = isActive.active;

            _context.Entry(job).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            var scheduler = await _schedulerFactory.GetScheduler();
            if (isActive.active)
            {
                await ScheduleJob(job);
            }
            else
            {
                var jobKey = new JobKey($"job-{job.Id}");
                await scheduler.DeleteJob(jobKey);
            }

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

        // Export scheduled jobs as a Sharp Scheduler cron file.
        [HttpGet("export")]
        public async Task<IActionResult> ExportJobsToCronFile()
        {
            // Fetch the scheduled jobs from the database and map them to a JobExportImportDTO.
            var jobs = await _context.ScheduledJobs
                .Select(j => new JobExportImportDTO
                {
                    Name = j.Name,
                    Command = j.Command,
                    CronExpression = j.CronExpression,
                    IsActive = j.IsActive
                })
                .ToListAsync();

            // StringBuilder is used to construct the cron file content.
            var cronContent = new StringBuilder();

            // Loop through each job and format its details into the cron content.
            foreach (var job in jobs)
            {
                // Add a comment line with the job name.
                cronContent.AppendLine($"# Name: {job.Name}");
                // Add the cron expression and command for the job, separated by '\t' (tab).
                cronContent.AppendLine($"{job.CronExpression}\t{job.Command}");
                // Add an empty line between jobs for better readability.
                cronContent.AppendLine();
            }

            // Generate a file name with the current UTC timestamp.
            var fileName = $"scheduled_jobs_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.cron";

            // Convert the cron content to a byte array.
            var fileBytes = Encoding.UTF8.GetBytes(cronContent.ToString());

            // Return the file as a response with text/plain MIME type.
            return File(fileBytes, "text/plain", fileName);
        }

        // This endpoint is used to import scheduled jobs from a Sharp Scheduler cron file.
        [HttpPost("import")]
        public async Task<IActionResult> ImportJobsFromCronFile(IFormFile file)
        {
            // Check if the file is null or empty.
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            // Read the contents of the uploaded file.
            using var reader = new StreamReader(file.OpenReadStream());
            var fileContent = await reader.ReadToEndAsync();

            // Split the file content into lines, removing empty entries.
            var lines = fileContent.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var jobsToImport = new List<ScheduledJobPostDTO>();

            // Loop through each line of the Sharp Scheduler cron file.
            for (int i = 0; i < lines.Length; i++)
            {
                // Check if the line starts with a comment and contains "Name:" (i.e., it's a job description).
                if (lines[i].StartsWith("#") && lines[i].Contains("Name:"))
                {
                    // Extract the job name from the comment line.
                    var jobName = lines[i].Replace("# Name:", "").Trim();

                    // Check if the next line contains the cron expression and command.
                    if (i + 1 < lines.Length)
                    {
                        var cronLine = lines[i + 1].Trim();
                        var parts = cronLine.Split(new[] { '\t' }, 2); // Split on '\t' (tab) to separate cron expression and command

                        // If the line has two parts (cron expression and command), add the job to the import list.
                        if (parts.Length == 2)
                        {
                            jobsToImport.Add(new ScheduledJobPostDTO
                            {
                                Name = jobName,
                                CronExpression = parts[0],
                                Command = parts[1],
                                IsActive = true // Default to active.
                            });

                            // Skip the next line since it's already processed.
                            i++;
                        }
                    }
                }
            }

            // Create the scheduled jobs in the database.
            return await CreateMany(jobsToImport);
        }

        [HttpGet("logs")]
        public async Task<IActionResult> GetJobLogs([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 1 : pageSize;
            pageSize = pageSize > 50 ? 50 : pageSize;

            var logsQuery = _context.JobExecutionLogs.AsQueryable();

            var totalLogs = await logsQuery.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalLogs / pageSize);

            var logs = await logsQuery
                .OrderByDescending(l => l.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var result = new
            {
                TotalLogs = totalLogs,
                TotalPages = totalPages,
                CurrentPage = page,
                Logs = logs
            };

            return Ok(result);
        }

        [HttpDelete("logs")]
        public async Task<IActionResult> DeleteJobLogs()
        {
            var logs = await _context.JobExecutionLogs.ToListAsync();

            if (!logs.Any())
            {
                return NotFound("No job execution logs found to delete.");
            }

            _context.JobExecutionLogs.RemoveRange(logs);
            await _context.SaveChangesAsync();

            return NoContent();
        }




        // Schedules a job with the Quartz scheduler based on the provided task's cron expression.
        // The job is linked to the specific task ID to execute the corresponding job logic.
        private async Task ScheduleJob(ScheduledJob task)
        {
            var scheduler = await _schedulerFactory.GetScheduler();

            // Create a new Quartz job, passing the task's ID to the job for execution.
            var job = JobBuilder.Create<JobExecutionService>()
                .WithIdentity($"job-{task.Id}")
                .UsingJobData("jobId", task.Id)
                .Build();

            // Create a trigger based on the task's cron expression to control the job's schedule.
            var trigger = TriggerBuilder.Create()
                .WithIdentity($"trigger-{task.Id}")
                .WithCronSchedule(task.CronExpression)
                .Build();

            // Schedule the job with the Quartz scheduler using the defined job and trigger.
            await scheduler.ScheduleJob(job, trigger);
        }

        // Validates the given cron expression to ensure it is properly formatted.
        // Returns true if the expression is valid, otherwise false.
        private bool IsValidCronExpression(string cronExpression)
        {
            try
            {
                // Validate the cron expression using Quartz's built-in validator.
                Quartz.CronExpression.ValidateExpression(cronExpression);
                return true;
            }
            catch (FormatException)
            {
                // Return false if the expression is invalid.
                return false;
            }
        }

    }
}
