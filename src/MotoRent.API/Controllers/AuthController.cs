using Microsoft.AspNetCore.Mvc;
using MotoRent.Application.Services;

namespace MotoRent.API.Controllers
{
    [ApiController]
    [ApiExplorerSettings(GroupName = @"Login")]
    [Route("login")]
    public class AuthController : ControllerBase
    {
        private readonly IJwtService _jwtService;

        public AuthController(IJwtService jwtService)
        {
            _jwtService = jwtService;
        }

        /// <summary>
        /// Realizar autenticação
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public IActionResult Login([FromBody] LoginModel model)
        {
            // TODO: Implement actual user authentication
            if (model.Username == "admin" && model.Password == "password")
            {
                var token = _jwtService.GenerateToken("admin-user-id", "Admin");
                return Ok(new { Token = token });
            }
            else if (model.Username.StartsWith("entregador") && model.Password == "password")
            {
                var token = _jwtService.GenerateToken($"{model.Username}-user-id", "Entregador");
                return Ok(new { Token = token });
            }

            return Unauthorized();
        }
    }

    public class LoginModel
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
}