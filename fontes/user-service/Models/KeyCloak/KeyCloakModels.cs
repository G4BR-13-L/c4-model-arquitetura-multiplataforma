using System.Text.Json.Serialization;

namespace UserService.API.Models.KeyCloak
{
    public sealed class CreateUserKeyCloakRequest
    {
        public string username { get; init; }
        public bool enabled { get; } = true;
        public string firstName { get; init; }
        public string lastName { get; init; }
        public string email { get; init; }
        public bool emailVerified { get; } = true;

        public static CreateUserKeyCloakRequest Create(User user)
        {
            return new CreateUserKeyCloakRequest()
            {
                username = user.UserName,
                firstName = user.FirstName,
                lastName = user.LastName,
                email = user.Email
            };
        }
    }

    public sealed class UpdateUserKeyCloakRequest
    {
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string email { get; set; }
    }

    public sealed record ChangePasswordKeyCloakRequest(string Value)
    {
        public string Type = "password";
        public bool Temporary = false;
    }

    public sealed class GetUserKeyCloakResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; init; }

        [JsonPropertyName("username")]
        public string UserName { get; init; }

        [JsonPropertyName("firstName")]
        public string FirstName { get; init; }

        [JsonPropertyName("lastName")]
        public string LastName { get; init; }

        [JsonPropertyName("email")]
        public string Email { get; init; }
    }
}
