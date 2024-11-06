namespace MoodzApi.Models;
public class Entry
{
    public string Text { get; set; } = null!;
    public Track Track { get; set; } = null!;
    public DateTime? Date { get; set; }
}