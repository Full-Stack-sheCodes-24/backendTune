namespace MoodzApi.Models;
public class RefreshTokenRequest
{
    public required string UserId { get; set; }
    public required string AccessToken { get; set; }
    public required string RefreshToken { get; set; }
}
