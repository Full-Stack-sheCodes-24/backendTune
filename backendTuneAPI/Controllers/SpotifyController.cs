using Microsoft.AspNetCore.Mvc;
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
}