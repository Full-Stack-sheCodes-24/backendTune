namespace MoodzApi.Models
{
    public class RefreshToken
    {
        public string? accessToken { get; set; }
        public DateTime? expiresAt { get; set; }
    }
}
