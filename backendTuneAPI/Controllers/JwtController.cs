using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MoodzApi.Models;
using MoodzApi.Services;

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
        public async Task<IActionResult> RefreshToken(RefreshTokenRequest request)
        {
            // Check if AccessToken is still valid
            if (!JwtTokenService.IsTokenExpired(request.AccessToken))
            {
                return BadRequest("JWT token has not expired. No refresh necessary");
            }

            // Check if RefreshToken is still valid
            if (await _usersService.IsRefreshTokenExpired(request.RefreshToken))
            {
                return Unauthorized("Invalid or expired refresh token. Please relogin");
            }

            // Generate a new Access Token
            return Ok(new RefreshTokenResponse {
                AccessToken = _jwtTokenService.GenerateToken(request.UserId),
                ExpiresIn = _jwtSettings.ExpiryMinutes
            });
        }

    }
}
