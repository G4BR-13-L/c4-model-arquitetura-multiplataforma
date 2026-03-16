using UserService.API.Models.KeyCloak;

namespace UserService.API.Models.Commands
{
    public sealed class UpdateUserCommand
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }

        public UpdateUserKeyCloakRequest ToKeyCloakRequest()
        {
            return new UpdateUserKeyCloakRequest
            {
                firstName = FirstName,
                lastName = LastName,
                email = Email
            };
        }
    }
}
