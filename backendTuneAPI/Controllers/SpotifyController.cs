using Microsoft.AspNetCore.Mvc;
using MoodzApi.Models;
using MoodzApi.Services;

namespace MoodzApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class SpotifyController : ControllerBase
{
    private readonly SpotifyService _spotifyService;
    public SpotifyController(SpotifyService spotifyService) =>
        _spotifyService = spotifyService;

    [HttpGet("search")]
    public async Task<IActionResult> Get([FromQuery] string query)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length > 256) return BadRequest("Query must be between 1 and 256 characters.");

        try {
            var searchResults = await _spotifyService.SearchTracks(query);
            return Content(searchResults, "application/json");
        } catch (Exception ex) {
            return BadRequest(ex.Message);
        }
    }

    // post api endpoint
    [HttpPost("{id:length(24)}/authcode")]
    public async Task<IActionResult> PostUserCode(string code, string id)
    {
        bool result = await _spotifyService.StoreAuthCodeAsync(code, id);

        if (result)
        {
            return Ok(result);
        }
        else
        {
            return NotFound();
        }
    }

    // This is just to test that the token is updated in the user doc, rn GetUserAccessToken is public but it should be private later
    [HttpPost("{id:length(24)}/token")]
    public async Task<IActionResult> PostUserToken(string userId)
    {
        try
    {
        // Call the GetUserAccessToken method in your service
        var accessToken = await _spotifyService.GetUserAccessToken(userId);

        // Return the access token as part of the response for testing purposes
        return Ok(new { AccessToken = accessToken.AccessToken, Expiration = accessToken.Expiration });
    }
    catch (Exception ex)
    {
        // Return any errors encountered during the process
        return StatusCode(500, $"Error retrieving access token: {ex.Message}");
    }
        
    }

    //get api endpoint to request most recently played tracks
    [HttpGet("recently-played/{userId}")]
    public async Task<IActionResult> GetRecentlyPlayed(string userId) 
    {
        try
        {
            // Call the service to get the most recent track
            var recentlyPlayedTracks = await _spotifyService.GetMostRecentTracks(userId);

            // Return the JSON response directly
            //return Ok(recentlyPlayedTracks);
            return Content(recentlyPlayedTracks, "application/json");
        }
        catch (Exception ex)
        {
            // Return an error response if the service call fails
            return StatusCode(500, $"Error retrieving recent tracks: {ex.Message}");
        }

    }

}