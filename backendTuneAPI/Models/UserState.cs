namespace MoodzApi.Models;
public class UserState
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string ProfilePicUrl { get; set; } = null!;
    public string BioText { get; set; } = null!;
    public DateTime? Birthday { get; set; }
    public List<Entry> Entries { get; set; } = new List<Entry>()!;
    public Auth Auth { get; set; } = new Auth();
    public Settings Settings { get; set; } = new Settings();
    public List<string> Followers { get; set; } = new List<string>();
    public List<string> Following { get; set; } = new List<string>();
    public List<FollowRequest> FollowRequests { get; set; } = new List<FollowRequest>();
}