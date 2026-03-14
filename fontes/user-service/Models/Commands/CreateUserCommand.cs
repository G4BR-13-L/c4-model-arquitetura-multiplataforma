namespace UserService.API.Models.Commands
{
    public sealed class CreateUserCommand
    {
        public string UserName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }

        public User ToModel()
        {
            return new User
            {
                UserName = UserName,
                FirstName = FirstName,
                LastName = LastName,
                Email = Email
            };
        }
    }
}
