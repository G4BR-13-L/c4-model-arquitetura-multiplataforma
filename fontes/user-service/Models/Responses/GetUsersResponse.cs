using System.Text.Json.Serialization;
using UserService.API.Models;

namespace UserService.API.Models.Responses
{
    public sealed class GetUsersResponse
    {
        [JsonPropertyName("id")]
        public Guid Id { get; init; }

        [JsonPropertyName("first_name")]
        public string FirstName { get; init; }

        [JsonPropertyName("last_name")]
        public string LastName { get; init; }

        [JsonPropertyName("email")]
        public string Email { get; init; }

        [JsonPropertyName("username")]
        public string Username { get; init; }

        [JsonPropertyName("document_number")]
        public string DocumentNumber { get; init; }

        public static GetUsersResponse FromUser(User user) => new()
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Username = user.UserName,
            DocumentNumber = user.DocumentNumber
        };
    }
}
