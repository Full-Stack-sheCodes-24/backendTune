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
    public DateTime? Birthday { get; set; }
    public List<Entry> Entries { get; set; } = new List<Entry>();
    public SpotifyUserAccessToken SpotifyUserAccessToken { get; set; } = null!;
    public string RefreshToken { get; set; } = null!;
    public DateTime? RefreshTokenExpiresAt { get; set; }
    public string SpotifyId { get; set; } = null!;
    public Settings Settings { get; set; } = new Settings();
}

public class ProfileInfo
{
    public string? ProfilePicUrl { get; set; }
    public string? BioText { get; set; }
    public DateTime? Birthday { get; set; }
}

public class Entry
{
    public string Text { get; set; } = null!;
    public int Likes { get; set; } = default!;
    public Track Track { get; set; } = null!;
    public DateTime? Date { get; set; } = null!;
}

public class Settings
{
    public string? Theme { get; set; }
    public bool? IsPrivate { get; set; }
}