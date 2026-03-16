using Microsoft.EntityFrameworkCore;
using UserService.API.Infra.Persistence;
using UserService.API.Models;
using UserService.API.Models.Commands;
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
        private readonly UserDbContext _dbContext;

        public UserService(IKeyCloakService keyCloakService, UserDbContext dbContext)
        {
            _keyCloakService = keyCloakService;
            _dbContext = dbContext;
        }

        public async Task<ServiceResult<User>> CreateUserAsync(CreateUserCommand request)
        {
            if (request is null)
                return ServiceResult<User>.Fail(StatusCodes.Status400BadRequest, "Payload da requisição inválido.");

            var model = request.ToModel();

            var userAlreadyExists = await _dbContext.Users.AnyAsync(x => x.UserName == model.UserName || x.Email == model.Email);
            if (userAlreadyExists)
                return ServiceResult<User>.Fail(StatusCodes.Status409Conflict, "Já existe um usuário com o mesmo nome de usuário ou e-mail.");

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

            _dbContext.Users.Add(model);

            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return ServiceResult<User>.Fail(StatusCodes.Status500InternalServerError, "Usuário criado no provedor de identidade, mas não foi possível persisti-lo no banco de dados.");
            }

            return ServiceResult<User>.Ok(model, "Usuário criado com sucesso.");
        }
    }
}
