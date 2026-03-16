using Microsoft.AspNetCore.Mvc;
using UserService.API.Infra.Repositories;
using UserService.API.Models;
using UserService.API.Models.Commands;

namespace UserService.API.Controllers
{
    [Route("v1/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IKeyCloakAuthRepository _keyCloakAuthRepository;

        public AuthController(IKeyCloakAuthRepository keyCloakAuthRepository)
        {
            _keyCloakAuthRepository = keyCloakAuthRepository;
        }

        [HttpPost("token")]
        public async Task<IActionResult> GenerateToken(GenerateTokenCommand command)
        {
            var tokenResult = await _keyCloakAuthRepository.GetTokenAsync(command.UserName, command.Password);
            if (tokenResult is null)
                return BadRequest(new { Message = "Usuário ou Senha inválidos!" });

            return Ok(tokenResult);
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenCommand command)
        {
            var result = await _keyCloakAuthRepository.RefreshTokenAsync(command.RefreshToken);
            if (result is null)
                return BadRequest(new { Message = "Token inválido ou expirado." });
            return Ok(result);
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] LogoutCommand command)
        {
            await _keyCloakAuthRepository.LogoutAsync(command.RefreshToken);
            return NoContent();
        }
    }
}
