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

    public Entry[] Entries { get; set; } = null!;
}

public class Entry
{
    public string Date { get; set; } = null!;
    public string SpotifyTrackID { get; set; } = null!;
    public string Text { get; set; } = null!;
    public int Likes { get; set; }
}