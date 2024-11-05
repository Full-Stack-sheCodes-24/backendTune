using Newtonsoft.Json;

namespace MoodzApi.Models;
public class SpotifyAccessToken
{
    [JsonProperty("access_token")]
    public string AccessToken { get; set; } = null!;

    [JsonProperty("token_type")]
    public string TokenType { get; set; } = null!;

    [JsonProperty("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonProperty("refresh_token")]
    public string RefreshToken { get; set; } = null!;

    public DateTime Expiration { get; private set; }


    //Calculates the Expiration time based on ExpiresIn
    public void SetExpiration()
    {
        Expiration = DateTime.UtcNow.AddSeconds(ExpiresIn);
    }
    public bool IsExpired()
    {
        if (AccessToken == null) return true; // No token available, considered expired

        return DateTime.UtcNow >= Expiration;
    }
}