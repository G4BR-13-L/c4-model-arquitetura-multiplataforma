using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using UserService.API.Models.Commands;
using UserService.API.Services;
using Microsoft.AspNetCore.Authorization;

namespace UserService.API.Controllers
{
    [Route("v1/users")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpPost]
        [Authorize(Roles = "service-admin")]
        public async Task<IActionResult> CreateUserAsync([FromBody]CreateUserCommand request)
        {
            var result = await _userService.CreateUserAsync(request);

            if (result.Success)
                return Ok(result.Data);

            if (result.StatusCode == StatusCodes.Status400BadRequest)
                return BadRequest(new { Message = result.Message });

            return StatusCode(result.StatusCode, new { Message = result.Message });
        }
    }
}
