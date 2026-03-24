using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserService.API.Infra.Repositories;
using UserService.API.Models;
using UserService.API.Models.Commands;

namespace UserService.API.Controllers
{
    [Route("v1/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IKeyCloakAuthRepository _keyCloakAuthRepository;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IKeyCloakAuthRepository keyCloakAuthRepository, ILogger<AuthController> logger)
        {
            _keyCloakAuthRepository = keyCloakAuthRepository;
            _logger = logger;
        }

        [HttpPost("token")]
        public async Task<IActionResult> GenerateToken(GenerateTokenCommand command)
        {
            _logger.LogInformation("Iniciando geração de token para o usuário {Usuario}", command.UserName);
            var tokenResult = await _keyCloakAuthRepository.GetTokenAsync(command.UserName, command.Password);
            if (tokenResult is null)
            {
                _logger.LogWarning("Falha na geração de token: usuário ou senha inválidos para {Usuario}", command.UserName);
                return BadRequest(new { Message = "Usuário ou Senha inválidos!" });
            }

            _logger.LogInformation("Token gerado com sucesso para o usuário {Usuario}", command.UserName);
            return Ok(tokenResult);
        }        
    }
}
