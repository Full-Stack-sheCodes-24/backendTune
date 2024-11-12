using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MoodzApi.Models;
using MoodzApi.Services;
using Newtonsoft.Json.Linq;

namespace MoodzApi.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class JwtController : ControllerBase
    {
        private readonly JwtTokenService _jwtTokenService;
        private readonly JwtSettings _jwtSettings;
        private readonly UsersService _usersService;
        public JwtController(JwtTokenService jwtTokenService, IOptions<JwtSettings> jwtSettings, UsersService usersService)
        {
            _jwtTokenService = jwtTokenService;
            _jwtSettings = jwtSettings.Value;
            _usersService = usersService;
        }

        //api endpoint to get a new jwt and refresh token
        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken(string refreshToken, Auth jwtToken)
        {
            if (!JwtTokenService.IsTokenExpired(jwtToken))
            {
                return BadRequest("JWT token has not expired. No refresh necessary");
            }

            var userId = await _usersService.ValidateRefreshToken(refreshToken);
            if (userId == null) return Unauthorized("Invalid or expired refresh token.");

            var newAuth = await _usersService.GenerateNewAuth(userId);

            return Ok(newAuth);
        }

    }
}
