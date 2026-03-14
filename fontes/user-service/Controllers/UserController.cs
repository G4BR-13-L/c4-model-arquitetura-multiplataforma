using Microsoft.AspNetCore.Mvc;
using System.Net;
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
        public async Task<IActionResult> CreateUserAsync([FromBody]CreateUserCommand request)
        {
            //Check if user already exists            
            var model = request.ToModel();

            var userCreated = await _keyCloakManagementRepository.CreateUserAsync(CreateUserKeyCloakRequest.Create(model));
            if (!userCreated)
               return StatusCode((int)HttpStatusCode.InternalServerError, "Erro ao criar usuário no idp.");

            var user = await _keyCloakManagementRepository.GetUserAsync(model.UserName);
            if (user is null)
                return StatusCode((int)HttpStatusCode.InternalServerError, "Erro ao buscar usuário no idp.");

            model.SetIdpId(user.Id);
            //salvar

            var passwordChanged = await _keyCloakManagementRepository.ResetPasswordAsync(user.Id, request.Password);
            if (!passwordChanged)
                return StatusCode((int)HttpStatusCode.InternalServerError, "Erro ao redefinir senha do usuário no idp.");

            return Ok(user);
        }
    }
}
