using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using MoodzApi.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace MoodzApi.Services
{
    public class JwtTokenService
    {
        private readonly JwtSettings _jwtSettings;
        public JwtTokenService(
            IOptions<JwtSettings> jwtSettings)
        {
            _jwtSettings = jwtSettings.Value;
        }

        //generate the jwt token for the user with corresponding id
        public string GenerateToken(string userId)
        {
            var claims = new[]
                {
                new Claim(JwtRegisteredClaimNames.Sub, userId),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: DateTime.Now.AddMinutes(_jwtSettings.ExpiryMinutes),
                signingCredentials: creds
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            return tokenString;
        }

        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                return Convert.ToBase64String(randomNumber);
            }
        }

        //required import to read token
        public static bool IsTokenExpired(Auth accessToken)
        {
            var handler = new JwtSecurityTokenHandler();

            // Check if the token is in a valid JWT format
            if (handler.CanReadToken(accessToken.AccessToken))
            {
                var token = handler.ReadJwtToken(accessToken.AccessToken);

                // Retrieve the expiration claim (exp)
                var expClaim = token.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Exp);

                if (expClaim != null && long.TryParse(expClaim.Value, out var exp))
                {
                    // Convert `exp` to DateTime
                    var expirationDateTime = DateTimeOffset.FromUnixTimeSeconds(exp).UtcDateTime;

                    // Check if the token is expired
                    return DateTime.UtcNow > expirationDateTime;
                }
            }
            // If token is not valid or missing exp claim, consider it expired
            return true;
        }
    }
}
