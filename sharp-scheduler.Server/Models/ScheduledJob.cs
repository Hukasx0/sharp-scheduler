namespace sharp_scheduler.Server.Models
{
    public class ScheduledJob
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Command { get; set; } = string.Empty;
        public string CronExpression { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? LastExecution { get; set; }
    }
}
