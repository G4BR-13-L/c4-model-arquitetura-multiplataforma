using System.Text.Json.Serialization;

namespace UserService.API.Models
{
    public sealed class AccessTokenResult
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; init; }
    }
}
