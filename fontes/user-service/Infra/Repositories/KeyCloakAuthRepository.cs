using Microsoft.Extensions.Options;
using System.Text.Json;
using UserService.API.Models;
using UserService.API.Models.KeyCloak;

namespace UserService.API.Infra.Repositories
{
    public interface IKeyCloakAuthRepository
    {
        Task<AccessTokenResult> GetTokenAsync();
        Task<AccessTokenResult> GetTokenAsync(string userName, string password);        
    }

    public sealed class KeyCloakAuthRepository : IKeyCloakAuthRepository
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly KeyCloakSettings _settings;

        public KeyCloakAuthRepository(
            IHttpClientFactory httpClientFactory,
            IOptions<KeyCloakSettings> settings)
        {
            _httpClientFactory = httpClientFactory;
            _settings = settings.Value;
        }

        public async Task<AccessTokenResult> GetTokenAsync()
        {
            var body = new Dictionary<string, string>()
            {
                { "grant_type", "client_credentials" },
                { "client_id", _settings.ClientId },
                { "client_secret", _settings.ClientSecret }
            };

            using var client = _httpClientFactory.CreateClient("KeyCloakAuth");

            var request = new HttpRequestMessage(HttpMethod.Post, $"protocol/openid-connect/token")
            {
                Content = new FormUrlEncodedContent(body)
            };

            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            if (!response.IsSuccessStatusCode)
                return null;

            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<AccessTokenResult>(responseContent);
        }
        public async Task<AccessTokenResult> GetTokenAsync(string userName, string password)
        {
            var body = new Dictionary<string, string>()
            {
                { "grant_type", "password" },
                { "client_id", _settings.ClientId },
                { "client_secret", _settings.ClientSecret },
                { "username", userName },
                { "password", password }
            };

            using var client = _httpClientFactory.CreateClient("KeyCloakAuth");

            var request = new HttpRequestMessage(HttpMethod.Post, $"protocol/openid-connect/token")
            {
                Content = new FormUrlEncodedContent(body)
            };

            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            if (!response.IsSuccessStatusCode)
                return null;

            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<AccessTokenResult>(responseContent);
        }
    }
}
