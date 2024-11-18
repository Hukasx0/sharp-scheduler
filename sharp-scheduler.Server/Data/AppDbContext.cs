using Microsoft.EntityFrameworkCore;
using sharp_scheduler.Server.Models;

namespace sharp_scheduler.Server.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> dbContextOptions) : base(dbContextOptions) { }

        public DbSet<ScheduledJob> ScheduledJobs { get; set; }
        public DbSet<Account> Accounts { get; set; }
        public DbSet<LoginLog> LoginLogs { get; set; }
        public DbSet<JobExecutionLog> JobExecutionLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ScheduledJob>()
                .HasKey(t => t.Id);

            modelBuilder.Entity<Account>()
                .HasKey(t => t.Id);

            modelBuilder.Entity<JobExecutionLog>()
                .HasIndex(l => l.JobId)
                .HasDatabaseName("Idx_JobId");

            modelBuilder.Entity<JobExecutionLog>()
                .HasIndex(l => l.Timestamp)
                .HasDatabaseName("Idx_JobExecution_Timestamp");

            modelBuilder.Entity<LoginLog>()
                .HasIndex(l => l.Timestamp)
                .HasDatabaseName("Idx_LoginLog_Timestamp");

            base.OnModelCreating(modelBuilder);
        }
    }
}
