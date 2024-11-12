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
        private readonly IMongoCollection<User> _usersCollection;
        public JwtTokenService(
            IOptions<JwtSettings> jwtSettings, IOptions<UserDatabaseSettings> userDatabaseSettings)
        {
            _jwtSettings = jwtSettings.Value;
            var mongoClient = new MongoClient(userDatabaseSettings.Value.ConnectionString);
            var mongoDatabase = mongoClient.GetDatabase(userDatabaseSettings.Value.DatabaseName);
            _usersCollection = mongoDatabase.GetCollection<User>(userDatabaseSettings.Value.UsersCollectionName);
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
        //generate the refresh token for the user with corresponding id
        public async Task<Auth> GenerateRefreshToken(string userId)
        {
            var randomNumber = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                var refreshTokenString = Convert.ToBase64String(randomNumber);

                var newRefreshToken = new Auth
                {
                    AccessToken = GenerateToken(userId),
                    RefreshToken = refreshTokenString,
                    ExpiresIn = _jwtSettings.ExpiryMinutes
                };

                var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
                var update = Builders<User>.Update
                    .Set(u => u.RefreshToken, refreshTokenString)
                    .Set(u => u.RefreshTokenExpiresAt, DateTime.Now.AddMinutes(newRefreshToken.ExpiresIn));

                await _usersCollection.UpdateOneAsync(filter, update);

                return newRefreshToken;
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

        //check for refresh token string among the users in the database
        public async Task<string> ValidateRefreshToken(string refreshToken)
        {
            var user = await _usersCollection.Find(x => x.RefreshToken == refreshToken).FirstOrDefaultAsync();
            if (user == null || user.RefreshTokenExpiresAt == null || user.RefreshTokenExpiresAt <= DateTime.UtcNow)
            {
                return null;
            }

            return user.Id;
        }
    }
}
