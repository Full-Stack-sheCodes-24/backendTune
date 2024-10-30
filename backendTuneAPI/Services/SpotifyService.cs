using Microsoft.Extensions.Options;
using MoodzApi.Models;
using Newtonsoft.Json;
using System.Net.Http.Headers;

namespace MoodzApi.Services;

public class SpotifyService
{
    private SpotifyAccessToken _accessToken;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly HttpClient _httpClient;
    private readonly UsersService _usersService;

    public SpotifyService(IOptions<SpotifyAuthSettings> spotifyAuthSettings, UsersService usersService)
    {
        _httpClient = new HttpClient();
        _clientId = spotifyAuthSettings.Value.ClientId;
        _clientSecret = spotifyAuthSettings.Value.ClientSecret;
        _accessToken = new SpotifyAccessToken();
        _usersService = usersService;
    }


    //Returns a new access token from spotify auth api
    private async Task<SpotifyAccessToken> GetAccessToken()
    {
        var requestBody = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "client_credentials"),
            new KeyValuePair<string, string>("client_id", _clientId),
            new KeyValuePair<string, string>("client_secret", _clientSecret)
        });

        var response = await _httpClient.PostAsync("https://accounts.spotify.com/api/token", requestBody);

        if (response.IsSuccessStatusCode) {
            var jsonResponse = await response.Content.ReadAsStringAsync();
            var tokenData = JsonConvert.DeserializeObject<SpotifyAccessToken>(jsonResponse);
            tokenData!.SetExpiration();

            return tokenData!;
        } else {
            // Handle errors (log, throw exception, etc.)
            throw new Exception("Could not retrieve access token");
        }
    }

    //Use at the beginning of every SpotifyApi call
    private void CheckAccessToken()
    {
        if (_accessToken.IsExpired()) _accessToken = GetAccessToken().Result;
    }

    public async Task<string> SearchTracks(string query)
    {
        CheckAccessToken();

        // Encode the query to handle special characters
        var encodedQuery = Uri.EscapeDataString(query);
        var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.spotify.com/v1/search?q={encodedQuery}&type=track");

        // Add the Authorization header to the request
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken.AccessToken);

        var response = await _httpClient.SendAsync(request);

        if (response.IsSuccessStatusCode) {
            var jsonResponse = await response.Content.ReadAsStringAsync();
            return jsonResponse; // This will return the search results in JSON format
        } else {
            throw new Exception($"Error searching tracks: {response.StatusCode} - {response.ReasonPhrase}");
        }
    }


    public async Task<bool> StoreAuthCodeAsync(string code, string userId)
    {
        return await _usersService.AddUserAuthCodeAsync(userId, code);
    }
}