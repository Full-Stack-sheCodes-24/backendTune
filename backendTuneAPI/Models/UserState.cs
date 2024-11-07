namespace MoodzApi.Models;
public class UserState
{
    public string UserId { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string ProfilePicUrl { get; set; } = null!;
    public string BioText { get; set; } = null!;
    public DateTime? Birthday { get; set; }
    public List<Entry> Entries { get; set; } = new List<Entry>()!;
    public Auth Auth { get; set; } = new Auth();
    public Settings Settings { get; set; } = new Settings();
}