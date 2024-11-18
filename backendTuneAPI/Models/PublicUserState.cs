namespace MoodzApi.Models;
public class PublicUserState : OtherUserState
{
    public string Name { get; set; } = null!;
    public string ProfilePicUrl { get; set; } = null!;
    public string BioText { get; set; } = null!;
    public DateTime? Birthday { get; set; }
    public List<Entry> Entries { get; set; } = new List<Entry>()!;
    public bool isPrivate = false;
}