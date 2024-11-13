namespace sharp_scheduler.Server.Models
{
    public class ScheduledJob
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Command { get; set; }
        public string CronExpression { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastRun {  get; set; }
        public string LastResult { get; set; }
        public bool IsActive { get; set; }
    }
}
