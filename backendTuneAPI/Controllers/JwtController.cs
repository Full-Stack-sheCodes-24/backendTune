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
        public async Task<IActionResult> RefreshToken(string refreshToken, Auth jwtToken)
        {
            if (!JwtTokenService.IsTokenExpired(jwtToken))
            {
                return BadRequest("JWT token has not expired. No refresh necessary");
            }

            var userId = await _jwtTokenService.ValidateRefreshToken(refreshToken);
            if (userId == null) return Unauthorized("Invalid or expired refresh token.");

            var newRefreshToken = await _jwtTokenService.GenerateRefreshToken(userId);

            return Ok(new
            {
                JwtToken = new Auth { AccessToken = newRefreshToken.AccessToken , RefreshToken = newRefreshToken.RefreshToken, ExpiresIn = newRefreshToken.ExpiresIn}
            });
        }

    }
}
