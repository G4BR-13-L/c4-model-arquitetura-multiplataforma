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

        public KeyCloakService(IKeyCloakManagementRepository keyCloakManagementRepository)
        {
            _keyCloakManagementRepository = keyCloakManagementRepository;
        }

        public async Task<bool> CreateUserAsync(User user)
        {
            return await _keyCloakManagementRepository.CreateUserAsync(CreateUserKeyCloakRequest.Create(user));
        }

        public async Task<GetUserKeyCloakResponse> GetUserByUsernameAsync(string username)
        {
            return await _keyCloakManagementRepository.GetUserAsync(username);
        }

        public async Task<bool> SetPasswordAsync(string userId, string password)
        {
            return await _keyCloakManagementRepository.ResetPasswordAsync(userId, password);
        }

        public async Task<bool> AssignRolesAsync(string userId, List<string> roleNames)
        {
            if (roleNames is null || roleNames.Count == 0)
                return true;

            var roles = new List<RoleRepresentation>();

            var normalizedRoleNames = roleNames
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var roleName in normalizedRoleNames)
            {
                var role = await _keyCloakManagementRepository.GetRoleAsync(roleName);
                if (role is not null)
                    roles.Add(role);
            }

            return await _keyCloakManagementRepository.AddRolesToUserAsync(userId, roles);
        }
    }
}
