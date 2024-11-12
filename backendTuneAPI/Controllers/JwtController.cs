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
        public JwtController(JwtTokenService jwtTokenService, IOptions<JwtSettings> jwtSettings)
        {
            _jwtTokenService = jwtTokenService;
            _jwtSettings = jwtSettings.Value;
        }

        //api endpoint to get a new jwt and refresh token
        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken(string refreshToken, JwtToken jwtToken)
        {
            if (!JwtTokenService.IsTokenExpired(jwtToken))
            {
                return BadRequest("JWT token has not expired. No refresh necessary");
            }

            var userId = await _jwtTokenService.ValidateRefreshToken(refreshToken);
            if (userId == null) return Unauthorized("Invalid or expired refresh token.");

            var newJwtToken = _jwtTokenService.GenerateToken(userId);
            var newRefreshToken = await _jwtTokenService.GenerateRefreshToken(userId);

            return Ok(new
            {
                JwtToken = new JwtToken { accessToken = newJwtToken },
                RefreshToken = new RefreshToken { accessToken = newRefreshToken.accessToken, expiresAt = newRefreshToken.expiresAt }
            });
        }

    }
}
