using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MoodzApi.Models;
using MoodzApi.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MoodzApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class JwtAuthController : ControllerBase
    {
        private readonly UsersService _usersService;
        public JwtAuthController(UsersService usersService)
        {
            _usersService = usersService;
        }

        [HttpPost("token")]
        public IActionResult GenerateJWTToken(string userId)
        {
            var tokenString = _usersService.GenerateToken(userId);

            return Ok(new { Token = tokenString });
        }
    }
}
