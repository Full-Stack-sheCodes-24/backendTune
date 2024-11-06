using Newtonsoft.Json;

namespace MoodzApi.Models;
public class SpotifyUserAccessToken : SpotifyAccessToken
{
    [JsonProperty("refresh_token")]
    public string RefreshToken { get; set; } = null!;

    [JsonProperty("scope")]
    public string Scope { get; set; } = null!;
}