namespace sharp_scheduler.Server.Models
{
    public class LoginLog
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? IpAddress { get; set; }
    }
}
