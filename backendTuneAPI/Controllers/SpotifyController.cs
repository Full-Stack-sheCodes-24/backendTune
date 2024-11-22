using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoodzApi.Services;

namespace MoodzApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class SpotifyController : ControllerBase
{
    private readonly SpotifyService _spotifyService;
    private readonly IUserContext _userContext;
    public SpotifyController(SpotifyService spotifyService, IUserContext userContext)
    {
        _spotifyService = spotifyService;
        _userContext = userContext;
    }

    [HttpGet("search")]
    [Authorize]
    public async Task<IActionResult> SearchSpotifyTracks([FromQuery] string query)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length > 256) return BadRequest("Query must be between 1 and 256 characters.");

        try {
            var searchResults = await _spotifyService.SearchTracks(query);
            return Ok(searchResults);
        } catch (Exception ex) {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("search/v2")]
    [Authorize]
    public async Task<IActionResult> SearchSpotifyTracksWithUserAccessToken([FromQuery] string query)
    {
        var userId = _userContext.UserId;

        if (string.IsNullOrWhiteSpace(query) || query.Length > 256) return BadRequest("Query must be between 1 and 256 characters.");

        try
        {
            var searchResults = await _spotifyService.SearchTracksWithUserAccessToken(query, userId);
            return Ok(searchResults);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("track/{trackId}")]
    [Authorize]
    public async Task<IActionResult> GetTrack(string trackId)
    {
        var userId = _userContext.UserId;

        try
        {
            var track = await _spotifyService.GetTrack(trackId, userId);
            return Ok(track);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    // This is just to test that the token is updated in the user doc,
    // [HttpPost("{id:length(24)}/token")]
    // public async Task<IActionResult> PostUserToken(string userId)
    // {
    //     try
    // {
    //     // Call the GetUserAccessToken method in your service
    //     var accessToken = await _spotifyService.CheckUserAccessToken(userId);

    //     // Return the access token as part of the response for testing purposes
    //     return Ok(new { AccessToken = accessToken.AccessToken, Expiration = accessToken.Expiration });
    // }
    // catch (Exception ex)
    // {
    //     // Return any errors encountered during the process
    //     return StatusCode(500, $"Error retrieving access token: {ex.Message}");
    // }

    // }

    //get api endpoint to request most recently played tracks

    [HttpGet("recently-played")]
    [Authorize]
    public async Task<IActionResult> GetRecentlyPlayed()
    {
        var userId = _userContext.UserId;

        try
        {
            // Call the service to get the most recent track
            var recentlyPlayedTracks = await _spotifyService.GetMostRecentTracks(userId);

            // Return the JSON response directly
            return Ok(recentlyPlayedTracks);
        }
        catch (Exception ex)
        {
            // Return an error response if the service call fails
            return StatusCode(500, $"Error retrieving recent tracks: {ex.Message}");
        }
    }

    [HttpGet("login/{authCode}")]
    public async Task<IActionResult> SpotifyUserLogin(string authCode)
    {
        try
        {
            // Call the service to get user state
            var userState = await _spotifyService.SpotifyUserLogin(authCode);

            // Return the JSON response directly
            return Ok(userState);
        }
        catch (Exception ex)
        {
            // Return an error response if the service call fails
            return StatusCode(500, $"Error logging in user: {ex.Message}");
        }
    }
}