using Microsoft.AspNetCore.Mvc;
using UserService.API.Infra;
using UserService.API.Infra.Repositories;
using UserService.API.Models.Commands;
using UserService.API.Models.KeyCloak;

namespace UserService.API.Controllers
{
    [Route("v1/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IKeyCloakManagementRepository _keyCloakManagementRepository;

        public UserController(IKeyCloakManagementRepository keyCloakManagementRepository)
        {
            _keyCloakManagementRepository = keyCloakManagementRepository;
        }

        [HttpPost]
        public async Task<IActionResult> CreateUserAsync([FromBody] CreateUserCommand request)
        {
            var model = request.ToModel();

            var userCreated = await _keyCloakManagementRepository.CreateUserAsync(
                CreateUserKeyCloakRequest.Create(model));

            // Idempotente: se já existe, retorna o usuário sem erro
            if (!userCreated)
            {
                var existingUser = await _keyCloakManagementRepository.GetUserAsync(model.UserName);
                if (existingUser is not null)
                    return Ok(existingUser);

                return new InternalServerError("Erro ao criar usuário no idp.");
            }

            var user = await _keyCloakManagementRepository.GetUserAsync(model.UserName);
            if (user is null)
                return new InternalServerError("Erro ao buscar usuário no idp.");

            model.SetIdpId(user.Id);

            var passwordChanged = await _keyCloakManagementRepository.ResetPasswordAsync(
                user.Id, request.Password);
            if (!passwordChanged)
                return new InternalServerError("Erro ao redefinir senha do usuário no idp.");

            return Ok(user);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetByIdAsync(string id)
        {
            var user = await _keyCloakManagementRepository.GetUserByIdAsync(id);
            if (user is null)
                return NotFound(new { message = $"Usuário com id '{id}' não encontrado." });

            return Ok(user);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAsync(string id, [FromBody] UpdateUserCommand request)
        {
            var existing = await _keyCloakManagementRepository.GetUserByIdAsync(id);
            if (existing is null)
                return NotFound(new { message = $"Usuário com id '{id}' não encontrado." });

            var updated = await _keyCloakManagementRepository.UpdateUserAsync(
                id, request.ToKeyCloakRequest());
            if (!updated)
                return new InternalServerError("Erro ao atualizar usuário no idp.");

            var user = await _keyCloakManagementRepository.GetUserByIdAsync(id);
            return Ok(user);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAsync(string id)
        {
            var existing = await _keyCloakManagementRepository.GetUserByIdAsync(id);
            if (existing is null)
                return NotFound(new { message = $"Usuário com id '{id}' não encontrado." });

            var deleted = await _keyCloakManagementRepository.DeleteUserAsync(id);
            if (!deleted)
                return new InternalServerError("Erro ao remover usuário no idp.");

            return NoContent();
        }
    }
}
