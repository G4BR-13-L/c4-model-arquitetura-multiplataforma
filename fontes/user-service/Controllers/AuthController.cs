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

        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenCommand command)
        {
            _logger.LogInformation("Iniciando renovação de token");
            var result = await _keyCloakAuthRepository.RefreshTokenAsync(command.RefreshToken);
            if (result is null)
            {
                _logger.LogWarning("Falha na renovação de token: token inválido ou expirado");
                return BadRequest(new { Message = "Token inválido ou expirado." });
            }

            _logger.LogInformation("Token renovado com sucesso");
            return Ok(result);
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] LogoutCommand command)
        {
            _logger.LogInformation("Iniciando logout do usuário");
            await _keyCloakAuthRepository.LogoutAsync(command.RefreshToken);
            _logger.LogInformation("Logout realizado com sucesso");
            return NoContent();
        }
    }
}
