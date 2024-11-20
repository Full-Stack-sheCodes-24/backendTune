namespace MoodzApi.Models;
public class FollowRequest
{
    public string FromUserId { get; set; } = null!;
    public string ToUserId { get; set; } = null!;
    public Status Status { get; set; }
}

public enum Status
{
    Pending,
    Success,
    FromBlocked,
    ToBlocked,
    BothBlocked
}