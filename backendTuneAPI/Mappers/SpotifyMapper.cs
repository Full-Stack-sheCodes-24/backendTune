using MoodzApi.Models;

namespace MoodzApi.Mappers;
public class SpotifyMapper
{
    public User SpotifyUserToUser(SpotifyUser spotifyUser)
    {
        return new User()
        {
            SpotifyId = spotifyUser.Id,
            Name = spotifyUser.DisplayName
        };
    }
}