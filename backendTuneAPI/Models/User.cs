using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MoodzApi.Models;
public class User
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    public string Email { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string ProfilePicUrl { get; set; } = null!;
    public string BioText { get; set; } = null!;
    public DateTime Birthday { get; set; } = default!;
    public List<Entry> Entries { get; set; } = new List<Entry>();
    public SpotifyUserAccessToken SpotifyUserAccessToken { get; set; } = null!;
    public string SpotifyId { get; set; } = null!;
}