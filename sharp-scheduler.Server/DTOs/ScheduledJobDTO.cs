namespace sharp_scheduler.Server.DTOs
{
    public class ScheduledJobDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Command { get; set; } = string.Empty;
        public string CronExpression { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? LastExecution { get; set; }
        public bool IsActive { get; set; }
    }

    public class ScheduledJobPostDTO
    {
        public string Name { get; set; } = string.Empty;
        public string Command { get; set; } = string.Empty;
        public string CronExpression { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class JobExportImportDTO
    {
        public string Name { get; set; } = string.Empty;
        public string Command { get; set; } = string.Empty;
        public string CronExpression { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class ActivateJobDTO
    {
        public bool active { get; set; }
    }

    public class ActivateManyJobDTO
    {
        public int Id { get; set; }
        public bool IsActive { get; set; }
    }
}
