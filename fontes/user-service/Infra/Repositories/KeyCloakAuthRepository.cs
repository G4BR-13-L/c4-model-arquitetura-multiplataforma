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
        Task<AccessTokenResult> RefreshTokenAsync(string refreshToken);
        Task LogoutAsync(string refreshToken);
    }

    public sealed class KeyCloakAuthRepository : IKeyCloakAuthRepository
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly KeyCloakSettings _settings;
        private readonly ILogger<KeyCloakAuthRepository> _logger;

        public KeyCloakAuthRepository(
            IHttpClientFactory httpClientFactory,
            IOptions<KeyCloakSettings> settings,
            ILogger<KeyCloakAuthRepository> logger)
        {
            _httpClientFactory = httpClientFactory;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<AccessTokenResult> GetTokenAsync()
        {
            _logger.LogInformation("Solicitando token via client_credentials");
            var body = new Dictionary<string, string>
            {
                { "grant_type", "client_credentials" },
                { "client_id", _settings.ClientId },
                { "client_secret", _settings.ClientSecret }
            };

            return await PostTokenAsync(body);
        }

        public async Task<AccessTokenResult> GetTokenAsync(string userName, string password)
        {
            _logger.LogInformation("Solicitando token via password para o usuário {Usuario}", userName);
            var body = new Dictionary<string, string>
            {
                { "grant_type", "password" },
                { "client_id", _settings.ClientId },
                { "client_secret", _settings.ClientSecret },
                { "username", userName },
                { "password", password }
            };

            return await PostTokenAsync(body);
        }

        public async Task<AccessTokenResult> RefreshTokenAsync(string refreshToken)
        {
            _logger.LogInformation("Solicitando renovação de token via refresh_token");
            var body = new Dictionary<string, string>
            {
                { "grant_type", "refresh_token" },
                { "client_id", _settings.ClientId },
                { "client_secret", _settings.ClientSecret },
                { "refresh_token", refreshToken }
            };

            return await PostTokenAsync(body);
        }

        public async Task LogoutAsync(string refreshToken)
        {
            _logger.LogInformation("Enviando requisição de logout para o KeyCloak");
            var body = new Dictionary<string, string>
            {
                { "client_id", _settings.ClientId },
                { "client_secret", _settings.ClientSecret },
                { "refresh_token", refreshToken }
            };

            using var client = _httpClientFactory.CreateClient("KeyCloakAuth");

            var request = new HttpRequestMessage(HttpMethod.Post, "protocol/openid-connect/logout")
            {
                Content = new FormUrlEncodedContent(body)
            };

            await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            _logger.LogInformation("Requisição de logout enviada com sucesso");
        }

        private async Task<AccessTokenResult> PostTokenAsync(Dictionary<string, string> body)
        {
            _logger.LogInformation("Enviando requisição de token para o KeyCloak");
            using var client = _httpClientFactory.CreateClient("KeyCloakAuth");

            var request = new HttpRequestMessage(HttpMethod.Post, "protocol/openid-connect/token")
            {
                Content = new FormUrlEncodedContent(body)
            };

            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Falha ao obter token do KeyCloak. StatusCode: {StatusCode}", response.StatusCode);
                return null;
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Token obtido com sucesso do KeyCloak");
            return JsonSerializer.Deserialize<AccessTokenResult>(responseContent);
        }
    }
}
