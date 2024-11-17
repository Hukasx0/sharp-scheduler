using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sharp_scheduler.Server.Data;
using sharp_scheduler.Server.DTOs;
using sharp_scheduler.Server.Models;
using System.Text.RegularExpressions;

namespace sharp_scheduler.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ScheduledJobsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ScheduledJobsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var jobs = await _context.ScheduledJobs.ToListAsync();
            return Ok(jobs);
        }

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

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var job = await _context.ScheduledJobs.FindAsync(id);
            if (job == null) return NotFound();

            _context.ScheduledJobs.Remove(job);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // import jobs from a cron file
        [HttpPost("import")]
        public async Task<IActionResult> ImportCronJobs(IFormFile cronFile)
        {
            if (cronFile == null || cronFile.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            var jobList = new List<ScheduledJob>();
            using (var reader = new StreamReader(cronFile.OpenReadStream()))
            {
                string line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    var match = Regex.Match(line, @"^(.*)\s+(.*)$");
                    if (match.Success)
                    {
                        var cronExpression = match.Groups[1].Value;
                        var command = match.Groups[2].Value;

                        jobList.Add(new ScheduledJob
                        {
                            Name = command.Split(' ')[0],
                            Command = command,
                            CronExpression = cronExpression,
                            CreatedAt = DateTime.UtcNow,
                            LastResult = ""
                        });
                    }
                }
            }

            await _context.ScheduledJobs.AddRangeAsync(jobList);
            await _context.SaveChangesAsync();

            return Ok(new { Message = $"{jobList.Count} jobs imported successfully." });
        }

        // export all jobs in a cron file
        [HttpGet("export")]
        public async Task<IActionResult> ExportCronJobs()
        {
            var jobs = await _context.ScheduledJobs.ToListAsync();
            var cronLines = new List<string>();

            foreach (var job in jobs)
            {
                var cronLine = $"{job.CronExpression} {job.Command}";
                cronLines.Add(cronLine);
            }

            var cronFileContent = string.Join("\n", cronLines);
            var fileBytes = System.Text.Encoding.UTF8.GetBytes(cronFileContent);

            return File(fileBytes, "text/plain", "cron_jobs.txt");
        }
    }
}
