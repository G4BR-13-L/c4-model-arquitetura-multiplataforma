using Microsoft.AspNetCore.WebUtilities;
using System.Text;
using System.Text.Json;
using UserService.API.Models.KeyCloak;

namespace UserService.API.Infra.Repositories
{
    public interface IKeyCloakManagementRepository
    {
        Task<bool> CreateUserAsync(CreateUserKeyCloakRequest user);
        Task<GetUserKeyCloakResponse> GetUserAsync(string username);
        Task<bool> ResetPasswordAsync(string userId, string newPassword);
    }

    public sealed class KeyCloakManagementRepository : IKeyCloakManagementRepository
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public KeyCloakManagementRepository(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<GetUserKeyCloakResponse> GetUserAsync(string username)
        {
            using var client = _httpClientFactory.CreateClient("KeyCloakAdmin");

            var uri = QueryHelpers.AddQueryString("users", "username", username);
            var request = new HttpRequestMessage(HttpMethod.Get, uri);

            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            if (!response.IsSuccessStatusCode)
                return null;

            var responseContent = await response.Content.ReadAsStringAsync();
            
            var list = JsonSerializer.Deserialize<List<GetUserKeyCloakResponse>>(responseContent);
            
            return list.FirstOrDefault();
        }
        public async Task<bool> CreateUserAsync(CreateUserKeyCloakRequest user)
        {
            using var client = _httpClientFactory.CreateClient("KeyCloakAdmin");

            var request = new HttpRequestMessage(HttpMethod.Post, $"users")
            {
                Content = new StringContent(JsonSerializer.Serialize(user), Encoding.UTF8, "application/json")
            };

            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            
            return response.IsSuccessStatusCode;
        }
        public async Task<bool> ResetPasswordAsync(string userId, string newPassword)
        {
            using var client = _httpClientFactory.CreateClient("KeyCloakAdmin");

            var body = new
            {
                type = "password",
                value = newPassword,
                temporary = false
            };

            var request = new HttpRequestMessage(HttpMethod.Put, $"users/{userId}/reset-password")
            {
                Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
            };

            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            return response.IsSuccessStatusCode;
        }
    }
}
