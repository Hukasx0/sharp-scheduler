namespace sharp_scheduler.Server.Models
{
    public class ScheduledJob
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Command { get; set; }
        public string CronExpression { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastExecution { get; set; }
        public string LastResult { get; set; }
        public string LastError { get; set; }
    }
}
