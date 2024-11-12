namespace MoodzApi.Models;
public class RefreshTokenResponse
{
    public string? AccessToken { get; set; }
    public int ExpiresIn { get; set; }
}
