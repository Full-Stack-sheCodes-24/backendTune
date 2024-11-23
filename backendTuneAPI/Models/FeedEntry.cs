using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MoodzApi.Models;
public class FeedEntry
{
    [BsonId]
    public ObjectId Id { get; set; }
    public string Name { get; set; } = null!;
    public string? ProfilePicUrl { get; set; }
    public string? Text { get; set; }
    public int? Likes { get; set; }
    public Track Track { get; set; } = null!;
    public DateTime Date { get; set; }
}