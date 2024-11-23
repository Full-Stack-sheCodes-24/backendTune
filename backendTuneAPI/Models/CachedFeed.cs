using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MoodzApi.Models;
public class CachedFeed
{
    [BsonId]
    public ObjectId Id { get; set; }
    public DateTime Expiration { get; set; }
    public List<FeedEntry> Feed { get; set; } = new List<FeedEntry>();
}