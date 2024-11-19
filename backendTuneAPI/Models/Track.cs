namespace MoodzApi.Models;
public class Track
{
    public string Name { get; set; } = null!;
    public string Uri { get; set; } = null!;
    public string Href { get; set; } = null!;
    public string Id { get; set; } = null!;
    public string? Preview_url { get; set; } = null!;
    public string? AlbumImageUrl { get; set; } = null;
}