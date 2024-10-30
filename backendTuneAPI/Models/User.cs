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
    public UserEntries[] Entries { get; set; } = null!;
    public string SpotifyAuthenticationCode { get; set; }
}