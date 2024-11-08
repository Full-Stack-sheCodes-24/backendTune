namespace MoodzApi.Models;
public class UserState
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string ProfilePicUrl { get; set; } = null!;
    public List<Entry> Entries { get; set; } = new List<Entry>()!;
    public string JwtToken { get; set; } = null!;
}