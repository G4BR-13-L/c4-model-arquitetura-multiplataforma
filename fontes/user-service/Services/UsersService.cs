using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text.Json;
using UserService.API.Infra.Notifications;
using UserService.API.Infra.Persistence;
using UserService.API.Models;
using UserService.API.Models.Commands;
using UserService.API.Models.DTOs;
using UserService.API.Models.Responses;
using UserService.API.Models.Services;

namespace UserService.API.Services
{
    public interface IUserService
    {
        Task<ServiceResult<User>> CreateUserAsync(CreateUserCommand request);
        Task<ServiceResult<IEnumerable<GetUsersResponse>>> GetAllUsersAsync();
        Task<ServiceResult<GetUsersResponse>> GetUserByIdAsync(Guid id);
    }

    public sealed class UsersService : IUserService
    {
        private readonly IKeyCloakService _keyCloakService;
        private readonly AppDbContext _dbContext;
        private readonly IEmailNotificationService _emailNotification;
        private readonly EmailNotificationOptions _emailOptions;
        private readonly ILogger<UsersService> _logger;

        public UsersService(
            IKeyCloakService keyCloakService,
            AppDbContext dbContext,
            IEmailNotificationService emailNotification,
            IOptions<EmailNotificationOptions> emailOptions,
            ILogger<UsersService> logger)
        {
            _keyCloakService = keyCloakService;
            _dbContext = dbContext;
            _emailNotification = emailNotification;
            _emailOptions = emailOptions.Value;
            _logger = logger;
        }

        public async Task<ServiceResult<IEnumerable<GetUsersResponse>>> GetAllUsersAsync()
        {
            _logger.LogInformation("Consultando todos os usuários no banco de dados");
            var users = await _dbContext.Users.ToListAsync();
            _logger.LogInformation("Consulta de usuários concluída. Total: {Total}", users.Count);
            return ServiceResult<IEnumerable<GetUsersResponse>>.Ok(users.Select(GetUsersResponse.FromUser));
        }

        public async Task<ServiceResult<GetUsersResponse>> GetUserByIdAsync(Guid id)
        {
            _logger.LogInformation("Consultando usuário com id {Id} no banco de dados", id);
            var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Id == id);
            if (user is null)
            {
                _logger.LogWarning("Usuário com id {Id} não encontrado no banco de dados", id);
                return ServiceResult<GetUsersResponse>.Fail(StatusCodes.Status404NotFound, $"Usu\u00e1rio com id '{id}' n\u00e3o encontrado.");
            }

            _logger.LogInformation("Usuário com id {Id} encontrado com sucesso", id);
            return ServiceResult<GetUsersResponse>.Ok(GetUsersResponse.FromUser(user));
        }

        public async Task<ServiceResult<User>> CreateUserAsync(CreateUserCommand request)
        {
            _logger.LogInformation("Iniciando processo de criação de usuário");

            if (request is null)
            {
                _logger.LogWarning("Payload da requisição é nulo");
                return ServiceResult<User>.Fail(StatusCodes.Status400BadRequest, "Payload da requisição inválido.");
            }

            var user = request.ToModel();

            _logger.LogInformation("Verificando se o usuário já existe no banco de dados");
            var userAlreadyExists = await _dbContext.Users.AnyAsync(x => x.UserName == user.UserName || x.Email == user.Email);
            if (userAlreadyExists)
            {
                _logger.LogWarning("Já existe um usuário com o mesmo nome de usuário ou e-mail");
                return ServiceResult<User>.Fail(StatusCodes.Status409Conflict, "Já existe um usuário com o mesmo nome de usuário ou e-mail.");
            }

            _logger.LogInformation("Criando usuário no provedor de identidade");
            var userCreated = await _keyCloakService.CreateUserAsync(user);
            if (!userCreated)
            {
                _logger.LogError("Falha ao criar usuário no provedor de identidade");
                return ServiceResult<User>.Fail(StatusCodes.Status500InternalServerError, "Falha ao criar usuário no provedor de identidade.");
            }

            _logger.LogInformation("Recuperando usuário criado no provedor de identidade");
            var keyCloakUser = await _keyCloakService.GetUserByUsernameAsync(user.UserName);
            if (user is null)
            {
                _logger.LogError("Usuário criado, mas não foi possível recuperá-lo no provedor de identidade");
                return ServiceResult<User>.Fail(StatusCodes.Status500InternalServerError, "Usuário criado, mas não foi possível recuperá-lo no provedor de identidade.");
            }

            user.SetIdpId(keyCloakUser.Id);

            _logger.LogInformation("Definindo senha do usuário no provedor de identidade");
            var passwordChanged = await _keyCloakService.SetPasswordAsync(keyCloakUser.Id, request.Password);
            if (!passwordChanged)
            {
                _logger.LogError("Usuário criado, mas não foi possível definir a senha no provedor de identidade");
                return ServiceResult<User>.Fail(StatusCodes.Status500InternalServerError, "Usuário criado, mas não foi possível definir a senha no provedor de identidade.");
            }

            _logger.LogInformation("Persistindo usuário no banco de dados");
            _dbContext.Users.Add(user);

            try
            {
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Usuário persistido no banco de dados com sucesso");
            }
            catch (DbUpdateException)
            {
                _logger.LogError("Falha ao persistir usuário no banco de dados");
                return ServiceResult<User>.Fail(StatusCodes.Status500InternalServerError, "Usuário criado no provedor de identidade, mas não foi possível persisti-lo no banco de dados.");
            }

            _logger.LogInformation("Enviando notificação por e-mail sobre a criação do usuário");
            await _emailNotification.SendAsync(
                recipientEmail: "system@user-service.com.br",
                recipientName: "System",
                subject: $"Usuário {user.UserName} criado com sucesso.",
                content: JsonSerializer.Serialize(UserCreatedDto.Create(user), new JsonSerializerOptions { WriteIndented = true }),
                queueName: _emailOptions.EmailNotificationQueueName);

            _logger.LogInformation("Usuário criado com sucesso");
            return ServiceResult<User>.Ok(user, "Usuário criado com sucesso.");
        }
    }
}
