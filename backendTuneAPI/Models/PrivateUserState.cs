namespace MoodzApi.Models;
public class PrivateUserState : OtherUserState
{
    public string Name { get; set; } = null!;
    public string ProfilePicUrl { get; set; } = null!;
    public bool isPrivate = true;
}