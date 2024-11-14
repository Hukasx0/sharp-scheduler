using Microsoft.EntityFrameworkCore;
using Quartz;
using sharp_scheduler.Server.Data;
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
            var taskId = context.JobDetail.JobDataMap.GetInt("taskId");
            var task = await _context.ScheduledJobs.FindAsync(taskId);

            if (task == null) return;

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                using var process = new Process();
                process.StartInfo = new ProcessStartInfo()
                {
                    FileName = GetShellCommand(),
                    Arguments = GetShellArguments(task.Command),
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                process.Start();
                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();


                task.LastRun = DateTime.UtcNow;
                task.LastResult = string.IsNullOrEmpty(error) ? output : error;


                _context.Entry(task).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private string GetShellCommand()
        {
            return OperatingSystem.IsWindows() ? "cmd.exe" : "/bin/bash";
        }

        private string GetShellArguments(string command)
        {
            return OperatingSystem.IsWindows() ? $"/c {command}" : $"-c \"{command}\"";
        }
    }
}
