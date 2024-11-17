using Microsoft.EntityFrameworkCore;
using sharp_scheduler.Server.Models;

namespace sharp_scheduler.Server.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> dbContextOptions) : base(dbContextOptions) { }

        public DbSet<ScheduledJob> ScheduledJobs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ScheduledJob>()
                .HasKey(t => t.Id);
        }
    }
}
