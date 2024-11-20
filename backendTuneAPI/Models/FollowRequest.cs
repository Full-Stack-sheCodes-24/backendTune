using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MoodzApi.Models;
public class FollowRequest
{
    public ObjectId FromUserId { get; set; }
    public ObjectId ToUserId { get; set; }
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