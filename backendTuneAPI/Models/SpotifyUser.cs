using Newtonsoft.Json;

namespace MoodzApi.Models;
public class SpotifyUser
{
    [JsonProperty("display_name")]
    public string DisplayName { get; set; } = null!;

    [JsonProperty("external_urls")]
    public ExternalUrls ExternalUrls { get; set; } = null!;

    [JsonProperty("followers")]
    public Followers Followers { get; set; } = null!;

    [JsonProperty("href")]
    public string Href { get; set; } = null!;

    [JsonProperty("id")]
    public string Id { get; set; } = null!;

    [JsonProperty("images")]
    public List<Image> Images { get; set; } = null!;

    [JsonProperty("type")]
    public string Type { get; set; } = null!;

    [JsonProperty("uri")]
    public string Uri { get; set; } = null!;
}

public class ExternalUrls
{
    [JsonProperty("spotify")]
    public string Spotify { get; set; } = null!;
}

public class Followers
{
    [JsonProperty("href")]
    public string Href { get; set; } = null!;

    [JsonProperty("total")]
    public int Total { get; set; }
}

public class Image
{
    [JsonProperty("url")]
    public string Url { get; set; } = null!;

    [JsonProperty("height")]
    public int? Height { get; set; }

    [JsonProperty("width")]
    public int? Width { get; set; }
}