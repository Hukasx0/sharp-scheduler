using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sharp_scheduler.Server.Data;
using sharp_scheduler.Server.DTOs;
using sharp_scheduler.Server.Models;

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
    }
}
