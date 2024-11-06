namespace MoodzApi.Models;
public class Entry
{
    public string Text { get; set; } = null!;
    public int Likes { get; set; } = default!;
    public Track Track { get; set; } = null!;
    public DateTime? Date { get; set; } = null!;
}