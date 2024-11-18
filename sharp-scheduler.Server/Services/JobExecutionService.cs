using Microsoft.EntityFrameworkCore;
using Quartz;
using sharp_scheduler.Server.Data;
using sharp_scheduler.Server.Models;
using System.Diagnostics;

namespace sharp_scheduler.Server.Services
{
    public class JobExecutionService : IJob
    {
        private readonly AppDbContext _context;

        public JobExecutionService(AppDbContext context)
        {
            _context = context;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var jobId = context.JobDetail.JobDataMap.GetInt("jobId");
            var job = await _context.ScheduledJobs.FindAsync(jobId);

            if (job == null) return;

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                using var process = new Process();
                process.StartInfo = new ProcessStartInfo()
                {
                    FileName = OperatingSystem.IsWindows() ? "cmd.exe" : "/bin/bash",
                    Arguments = OperatingSystem.IsWindows() ? $"/c {job.Command}" : $"-c \"{job.Command}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                process.Start();

                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();

                await process.WaitForExitAsync();

                job.LastExecution = DateTime.UtcNow;

                /*var jobExecutionLog = new JobExecutionLog
                {
                    JobId = job.Id,
                    Timestamp = DateTime.UtcNow,
                    Output = output,
                    Error = error,
                    Status = string.IsNullOrEmpty(error) ? "Success" : "Failure"
                };*/

                _context.Entry(job).State = EntityState.Modified;
              //  _context.JobExecutionLogs.Add(jobExecutionLog);

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($"Error executing job {jobId}: {ex.Message}");
                throw;
            }
        }
    }
}
