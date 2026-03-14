using UserService.API.Models;
using UserService.API.Models.Commands;
using UserService.API.Models.KeyCloak;
using UserService.API.Models.Services;

namespace UserService.API.Services
{
    public interface IUserService
    {
        Task<ServiceResult<User>> CreateUserAsync(CreateUserCommand request);
    }

    public sealed class UserService : IUserService
    {
        private readonly IKeyCloakService _keyCloakService;

        public UserService(IKeyCloakService keyCloakService)
        {
            _keyCloakService = keyCloakService;
        }

        public async Task<ServiceResult<User>> CreateUserAsync(CreateUserCommand request)
        {
            if (request is null)
                return ServiceResult<User>.Fail(StatusCodes.Status400BadRequest, "Payload da requisição inválido.");

            var model = request.ToModel();

            var userCreated = await _keyCloakService.CreateUserAsync(model);
            if (!userCreated)
                return ServiceResult<User>.Fail(StatusCodes.Status500InternalServerError, "Falha ao criar usuário no provedor de identidade.");

            var user = await _keyCloakService.GetUserByUsernameAsync(model.UserName);
            if (user is null)
                return ServiceResult<User>.Fail(StatusCodes.Status500InternalServerError, "Usuário criado, mas não foi possível recuperá-lo no provedor de identidade.");

            model.SetIdpId(user.Id);

            var passwordChanged = await _keyCloakService.SetPasswordAsync(user.Id, request.Password);
            if (!passwordChanged)
                return ServiceResult<User>.Fail(StatusCodes.Status500InternalServerError, "Usuário criado, mas não foi possível definir a senha no provedor de identidade.");

            if (request.Roles is not null && request.Roles.Count > 0)
            {
                var rolesAdded = await _keyCloakService.AssignRolesAsync(user.Id, request.Roles);
                if (!rolesAdded)
                    return ServiceResult<User>.Fail(StatusCodes.Status500InternalServerError, "Usuário criado, mas não foi possível atribuir os perfis no provedor de identidade.");
            }

            return ServiceResult<User>.Ok(model, "Usuário criado com sucesso.");
        }
    }
}
