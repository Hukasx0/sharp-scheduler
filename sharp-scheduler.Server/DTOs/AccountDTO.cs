namespace sharp_scheduler.Server.DTOs
{
    public class AccountDTO
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
    }
    public class LoginDTO
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class ChangePasswordDTO
    {
        public string OldPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }

    public class DeleteAccountDTO
    {
        public string Password { get; set; } = string.Empty;
    }

    public class CreateNewAccountDTO
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
