namespace UserService.API.Models
{
    public sealed record GenerateTokenCommand(string UserName, string Password);    
}
