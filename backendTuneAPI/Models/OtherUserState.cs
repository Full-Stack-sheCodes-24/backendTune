namespace MoodzApi.Models;
public class OtherUserState
{
    public string Id { get; set; } = null!;
    public List<string> Following = new List<string>();
    public List<string> Followers = new List<string>();
}