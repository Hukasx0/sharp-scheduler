﻿namespace sharp_scheduler.Server.Models
{
    public class JobExecutionLog
    {
        public int Id { get; set; }
        public int JobId { get; set; }
        public string JobName { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string Output { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}
