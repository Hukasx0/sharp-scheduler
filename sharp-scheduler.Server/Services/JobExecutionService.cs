using Microsoft.EntityFrameworkCore;
using Quartz;
using sharp_scheduler.Server.Data;
using sharp_scheduler.Server.Models;
using System.Diagnostics;

namespace sharp_scheduler.Server.Services
{
    // Executes scheduled jobs by running the specified command. 
    // Logs the output and errors of the job execution into the database, updating the job's last execution timestamp.
    public class JobExecutionService : IJob
    {
        private readonly AppDbContext _context;

        // Constructor initializes the service with the required DbContext
        public JobExecutionService(AppDbContext context)
        {
            _context = context;
        }

        // Executes the job by retrieving job details from the database and running the specified command.
        // Logs the job execution output and status (success or failure) into the database.
        // If an error occurs, a rollback is performed to ensure the database is in a consistent state.
        public async Task Execute(IJobExecutionContext context)
        {
            var jobId = context.JobDetail.JobDataMap.GetInt("jobId");
            var job = await _context.ScheduledJobs.FindAsync(jobId);

            // If the job is not found, return without further processing
            if (job == null) return;

            // Start a new database transaction to ensure job execution and logging are consistent
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Setup process to execute the job's command (cross-platform handling for Windows/Linux)
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

                // Capture the output and error of the job execution
                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();

                // Wait for the process to exit before continuing
                await process.WaitForExitAsync();

                // Update job's last execution timestamp in the database
                job.LastExecution = DateTime.UtcNow;

                // Log job execution details (output, error, and status) into the database
                var jobExecutionLog = new JobExecutionLog
                {
                    JobId = job.Id,
                    JobName = job.Name,
                    Timestamp = DateTime.UtcNow,
                    Output = output,
                    Error = error,
                    Status = string.IsNullOrEmpty(error) ? "Success" : "Failure"
                };

                // Mark job as modified and add the execution log entry
                _context.Entry(job).State = EntityState.Modified;
                _context.JobExecutionLogs.Add(jobExecutionLog);

                // Commit the transaction to persist changes
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                // Rollback transaction in case of an error to maintain consistency
                await transaction.RollbackAsync();
                Console.WriteLine($"Error executing job {jobId}: {ex.Message}");
                throw;
            }
        }
    }
}
