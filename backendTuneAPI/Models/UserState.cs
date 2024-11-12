namespace MoodzApi.Models;
public class UserState
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string ProfilePicUrl { get; set; } = null!;
    public string BioText { get; set; } = null!;
    public DateTime? Birthday { get; set; }
    public List<Entry> Entries { get; set; } = new List<Entry>()!;
    public JwtToken JwtToken { get; set; } = new JwtToken();
    public RefreshToken RefreshToken { get; set; } = new RefreshToken();
}