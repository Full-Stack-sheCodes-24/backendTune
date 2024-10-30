namespace MoodzApi.Models
{
    public class UserEntries
    {
        public string Date { get; set; } = null!;
        public string SpotifyTrackID { get; set; } = null!;
        public string Text { get; set; } = null!;
        public int Likes { get; set; }
    }
}
