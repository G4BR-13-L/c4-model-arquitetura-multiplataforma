namespace UserService.API.Models.Commands
{
    public sealed class LogoutCommand
    {
        public string RefreshToken { get; set; }
    }
}
