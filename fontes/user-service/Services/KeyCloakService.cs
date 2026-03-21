using System.Data;
using UserService.API.Infra.Repositories;
using UserService.API.Models;
using UserService.API.Models.KeyCloak;

namespace UserService.API.Services
{
    public interface IKeyCloakService
    {
        Task<bool> CreateUserAsync(User user);
        Task<GetUserKeyCloakResponse> GetUserByUsernameAsync(string username);
        Task<bool> SetPasswordAsync(string userId, string password);
        Task<bool> AssignRolesAsync(string userId, List<string> roleNames);
    }
    public sealed class KeyCloakService : IKeyCloakService
    {
        private readonly IKeyCloakManagementRepository _keyCloakManagementRepository;
        private readonly ILogger<KeyCloakService> _logger;

        public KeyCloakService(IKeyCloakManagementRepository keyCloakManagementRepository, ILogger<KeyCloakService> logger)
        {
            _keyCloakManagementRepository = keyCloakManagementRepository;
            _logger = logger;
        }

        public async Task<bool> CreateUserAsync(User user)
        {
            _logger.LogInformation("Enviando requisição para criar usuário no KeyCloak");
            var result = await _keyCloakManagementRepository.CreateUserAsync(CreateUserKeyCloakRequest.Create(user));
            _logger.LogInformation("Resultado da criação do usuário no KeyCloak: {Resultado}", result);
            return result;
        }

        public async Task<GetUserKeyCloakResponse> GetUserByUsernameAsync(string username)
        {
            _logger.LogInformation("Buscando usuário por nome de usuário no KeyCloak: {Username}", username);
            var result = await _keyCloakManagementRepository.GetUserAsync(username);
            _logger.LogInformation("Busca de usuário no KeyCloak concluída. Encontrado: {Encontrado}", result is not null);
            return result;
        }

        public async Task<bool> SetPasswordAsync(string userId, string password)
        {
            _logger.LogInformation("Definindo senha para o usuário {UserId} no KeyCloak", userId);
            var result = await _keyCloakManagementRepository.ResetPasswordAsync(userId, password);
            _logger.LogInformation("Resultado da definição de senha no KeyCloak: {Resultado}", result);
            return result;
        }

        public async Task<bool> AssignRolesAsync(string userId, List<string> roleNames)
        {
            _logger.LogInformation("Iniciando atribuição de roles para o usuário {UserId}", userId);

            if (roleNames is null || roleNames.Count == 0)
            {
                _logger.LogInformation("Nenhuma role informada para atribuição");
                return true;
            }

            var roles = new List<RoleRepresentation>();

            var normalizedRoleNames = roleNames
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            _logger.LogInformation("Buscando {Total} roles no KeyCloak", normalizedRoleNames.Count);
            foreach (var roleName in normalizedRoleNames)
            {
                _logger.LogInformation("Buscando role {RoleName} no KeyCloak", roleName);
                var role = await _keyCloakManagementRepository.GetRoleAsync(roleName);
                if (role is not null)
                    roles.Add(role);
                else
                    _logger.LogWarning("Role {RoleName} não encontrada no KeyCloak", roleName);
            }

            _logger.LogInformation("Atribuindo {Total} roles ao usuário {UserId}", roles.Count, userId);
            var result = await _keyCloakManagementRepository.AddRolesToUserAsync(userId, roles);
            _logger.LogInformation("Resultado da atribuição de roles: {Resultado}", result);
            return result;
        }
    }
}
