using System.Text.Json.Serialization;
using UserService.API.Models.KeyCloak;

namespace UserService.API.Models.Commands
{
    public sealed class CreateUserCommand
    {
        [JsonPropertyName("first_name")]
        public string FirstName { get; set; }

        [JsonPropertyName("last_name")]
        public string LastName { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("username")]
        public string UserName { get; set; }

        [JsonPropertyName("password")]
        public string Password { get; set; }

        [JsonPropertyName("document_number")]
        public string DocumentNumber { get; set; }

        public User ToModel()
        {
            return new User
            {
                UserName = UserName,
                FirstName = FirstName,
                LastName = LastName,
                Email = Email,
                DocumentNumber = DocumentNumber
            };
        }
    }
}
