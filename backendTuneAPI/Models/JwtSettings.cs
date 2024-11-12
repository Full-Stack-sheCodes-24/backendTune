namespace MoodzApi.Models;

public class JwtSettings
{
    public string Issuer { get; set; } = null!;
    public string Audience { get; set; } = null!;
    public string SecretKey { get; set; } = null!;
    public int ExpiryMinutes { get; set; }
    public int RefreshTokenExpiryDays {  get; set; }
}
