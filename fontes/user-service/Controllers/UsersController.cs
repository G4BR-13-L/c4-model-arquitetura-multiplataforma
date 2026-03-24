using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserService.API.Models.Commands;
using UserService.API.Models.Responses;
using UserService.API.Services;

namespace UserService.API.Controllers
{
    [Route("v1/users")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IValidator<CreateUserCommand> _validator;
        private readonly ILogger<UsersController> _logger;

        public UsersController(IUserService userService, IValidator<CreateUserCommand> validator, ILogger<UsersController> logger)
        {
            _userService = userService;
            _validator = validator;
            _logger = logger;
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetAllUsersAsync()
        {
            _logger.LogInformation("Iniciando busca de todos os usuários");
            var result = await _userService.GetAllUsersAsync();
            _logger.LogInformation("Busca de todos os usuários concluída com sucesso");
            return Ok(result.Data);
        }

        [Authorize]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserByIdAsync(Guid id)
        {
            _logger.LogInformation("Iniciando busca do usuário com id {Id}", id);
            var result = await _userService.GetUserByIdAsync(id);

            if (result.Success)
            {
                _logger.LogInformation("Usuário com id {Id} encontrado com sucesso", id);
                return Ok(result.Data);
            }

            if (result.StatusCode == StatusCodes.Status404NotFound)
            {
                _logger.LogWarning("Usuário com id {Id} não encontrado", id);
                return NotFound(new { Message = result.Message });
            }

            _logger.LogError("Erro ao buscar usuário com id {Id}: {Mensagem}", id, result.Message);
            return StatusCode(result.StatusCode, new { Message = result.Message });
        }

        [HttpPost]
        public async Task<IActionResult> CreateUserAsync([FromBody]CreateUserCommand request)
        {
            _logger.LogInformation("Iniciando criação de usuário");

            var validation = await _validator.ValidateAsync(request);
            if (!validation.IsValid)
            {
                var errors = validation.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

                _logger.LogWarning("Validação falhou ao criar usuário: {Erros}", errors);
                return BadRequest(new { Errors = errors });
            }

            var result = await _userService.CreateUserAsync(request);

            if (result.Success)
            {
                _logger.LogInformation("Usuário criado com sucesso");
                return StatusCode(StatusCodes.Status201Created, CreateUserResponse.FromUser(result.Data));
            }

            if (result.StatusCode == StatusCodes.Status400BadRequest)
            {
                _logger.LogWarning("Requisição inválida ao criar usuário: {Mensagem}", result.Message);
                return BadRequest(new { Message = result.Message });
            }

            _logger.LogError("Erro ao criar usuário: {Mensagem}", result.Message);
            return StatusCode(result.StatusCode, new { Message = result.Message });
        }
    }
}
