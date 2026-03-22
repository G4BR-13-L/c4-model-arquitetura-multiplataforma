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
        Task<GetUserKeyCloakResponse> GetUserByIdAsync(string userId);
        Task<bool> UpdateUserAsync(string userId, UpdateUserKeyCloakRequest user);
        Task<bool> DeleteUserAsync(string userId);
        Task<bool> ResetPasswordAsync(string userId, string newPassword);
        Task<RoleRepresentation> GetRoleAsync(string roleName);
        Task<bool> AddRolesToUserAsync(string userId, List<RoleRepresentation> roles);
    }

    public sealed class KeyCloakManagementRepository : IKeyCloakManagementRepository
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<KeyCloakManagementRepository> _logger;

        public KeyCloakManagementRepository(IHttpClientFactory httpClientFactory, ILogger<KeyCloakManagementRepository> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<GetUserKeyCloakResponse> GetUserAsync(string username)
        {
            _logger.LogInformation("Enviando requisição GET para buscar usuário por username no KeyCloak");
            using var client = _httpClientFactory.CreateClient("KeyCloakAdmin");

            var uri = QueryHelpers.AddQueryString("users", "username", username);
            var request = new HttpRequestMessage(HttpMethod.Get, uri);

            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Falha ao buscar usuário no KeyCloak. StatusCode: {StatusCode}", response.StatusCode);
                return null;
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var list = JsonSerializer.Deserialize<List<GetUserKeyCloakResponse>>(responseContent);

            _logger.LogInformation("Busca de usuário no KeyCloak concluída com sucesso");
            return list.FirstOrDefault();
        }

        public async Task<GetUserKeyCloakResponse> GetUserByIdAsync(string userId)
        {
            _logger.LogInformation("Enviando requisição GET para buscar usuário por id no KeyCloak");
            using var client = _httpClientFactory.CreateClient("KeyCloakAdmin");

            var request = new HttpRequestMessage(HttpMethod.Get, $"users/{userId}");
            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Falha ao buscar usuário por id no KeyCloak. StatusCode: {StatusCode}", response.StatusCode);
                return null;
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Busca de usuário por id no KeyCloak concluída com sucesso");
            return JsonSerializer.Deserialize<GetUserKeyCloakResponse>(responseContent);
        }

        public async Task<bool> CreateUserAsync(CreateUserKeyCloakRequest user)
        {
            _logger.LogInformation("Enviando requisição POST para criar usuário no KeyCloak");
            using var client = _httpClientFactory.CreateClient("KeyCloakAdmin");

            var request = new HttpRequestMessage(HttpMethod.Post, "users")
            {
                Content = new StringContent(JsonSerializer.Serialize(user), Encoding.UTF8, "application/json")
            };

            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            _logger.LogInformation("Resultado da criação de usuário no KeyCloak. StatusCode: {StatusCode}", response.StatusCode);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateUserAsync(string userId, UpdateUserKeyCloakRequest user)
        {
            _logger.LogInformation("Enviando requisição PUT para atualizar usuário no KeyCloak");
            using var client = _httpClientFactory.CreateClient("KeyCloakAdmin");

            var request = new HttpRequestMessage(HttpMethod.Put, $"users/{userId}")
            {
                Content = new StringContent(JsonSerializer.Serialize(user), Encoding.UTF8, "application/json")
            };

            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            _logger.LogInformation("Resultado da atualização de usuário no KeyCloak. StatusCode: {StatusCode}", response.StatusCode);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteUserAsync(string userId)
        {
            _logger.LogInformation("Enviando requisição DELETE para remover usuário no KeyCloak");
            using var client = _httpClientFactory.CreateClient("KeyCloakAdmin");

            var request = new HttpRequestMessage(HttpMethod.Delete, $"users/{userId}");
            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            _logger.LogInformation("Resultado da remoção de usuário no KeyCloak. StatusCode: {StatusCode}", response.StatusCode);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> ResetPasswordAsync(string userId, string newPassword)
        {
            _logger.LogInformation("Enviando requisição PUT para redefinir senha do usuário no KeyCloak");
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

            _logger.LogInformation("Resultado da redefinição de senha no KeyCloak. StatusCode: {StatusCode}", response.StatusCode);
            return response.IsSuccessStatusCode;
        }

        public async Task<RoleRepresentation> GetRoleAsync(string roleName)
        {
            _logger.LogInformation("Enviando requisição GET para buscar role {RoleName} no KeyCloak", roleName);
            using var client = _httpClientFactory.CreateClient("KeyCloakAdmin");

            var request = new HttpRequestMessage(HttpMethod.Get, $"roles/{roleName}");

            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Falha ao buscar role {RoleName} no KeyCloak. StatusCode: {StatusCode}", roleName, response.StatusCode);
                return null;
            }

            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("Role {RoleName} encontrada com sucesso no KeyCloak", roleName);
            return JsonSerializer.Deserialize<RoleRepresentation>(responseContent);
        }

        public async Task<bool> AddRolesToUserAsync(string userId, List<RoleRepresentation> roles)
        {
            _logger.LogInformation("Enviando requisição POST para atribuir roles ao usuário {UserId} no KeyCloak", userId);
            using var client = _httpClientFactory.CreateClient("KeyCloakAdmin");

            var request = new HttpRequestMessage(HttpMethod.Post, $"users/{userId}/role-mappings/realm")
            {
                Content = new StringContent(JsonSerializer.Serialize(roles), Encoding.UTF8, "application/json")
            };

            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            _logger.LogInformation("Resultado da atribuição de roles no KeyCloak. StatusCode: {StatusCode}", response.StatusCode);
            return response.IsSuccessStatusCode;
        }
    }
}
