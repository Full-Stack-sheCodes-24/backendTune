using Microsoft.Extensions.Options;
using MoodzApi.Models;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

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

    // User access token swapping
    private async Task<SpotifyAccessToken> GetUserAccessToken(string userId) {

        // POST request to /api/token endpoint
        string code = await _usersService.GetSpotifyAuthorizationCode(userId);
        const string redirect_uri = "http://localhost:5173/callback";  // probably store it somewhere else later?

        if (code == null)
        {
            throw new Exception("Authorization code not found for user.");
        }

        var authHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_clientId}:{_clientSecret}"));
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authHeader);

        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "authorization_code"),
            new KeyValuePair<string, string>("code", code),
            new KeyValuePair<string, string>("redirect_uri", redirect_uri)
        });
        
        var response = await _httpClient.PostAsync("https://accounts.spotify.com/api/token", content);

        if (response.IsSuccessStatusCode) {
            var jsonResponse = await response.Content.ReadAsStringAsync();
            Console.WriteLine(jsonResponse);
            var tokenData = JsonConvert.DeserializeObject<SpotifyAccessToken>(jsonResponse);
            tokenData!.SetExpiration();
            
            // store access token in user doc
            await _usersService.UpdateSpotifyAccessToken(userId, tokenData);
            
            // Return true if the update was successful
            return tokenData!;

        } 
        else
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"Could not retrieve access token: {response.StatusCode} - {errorContent}");
        }
  
    }

    // Will be used in API calls
    private async void CheckUserAccessToken(string userId) {
        SpotifyAccessToken userToken = await _usersService.GetSpotifyAccessToken(userId);
        if (userToken == null) {
             await GetUserAccessToken(userId);
        }
        else {
            if (userToken.IsExpired()) {
             await RefreshAccessToken(userId, userToken);
            }
        }
    }

    // Called when userToken is expired, returns a new access token
    private async Task<SpotifyAccessToken> RefreshAccessToken(string userId, SpotifyAccessToken userToken) {

        string refresh_token = userToken.RefreshToken; //gets refresh token from the user doc
        var authHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_clientId}:{_clientSecret}"));
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authHeader);

        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "refresh_token"),
            new KeyValuePair<string, string>("refresh_token", refresh_token),
        });
        
        var response = await _httpClient.PostAsync("https://accounts.spotify.com/api/token", content);
        
        if (response.IsSuccessStatusCode) {
            var jsonResponse = await response.Content.ReadAsStringAsync();
            var tokenData = JsonConvert.DeserializeObject<SpotifyAccessToken>(jsonResponse);
            tokenData!.SetExpiration();

            if (tokenData!.RefreshToken == null) {
                tokenData!.RefreshToken = refresh_token;
            }

            // store new access token in user doc
            await _usersService.UpdateSpotifyAccessToken(userId, tokenData);

            return tokenData!;
        } else {
            // Handle errors (log, throw exception, etc.)
            throw new Exception("Could not retrieve access token using refresh token");
        }
    }

    // get recently played api request
    public async Task<string> GetMostRecentTracks(string userId) {
    
         CheckUserAccessToken(userId);

        SpotifyAccessToken userAccessToken = await _usersService.GetSpotifyAccessToken(userId);
        
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.spotify.com/v1/me/player/recently-played?limit=1");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userAccessToken.AccessToken);

        var response = await _httpClient.SendAsync(request);

        if (response.IsSuccessStatusCode) {
            var jsonResponse = await response.Content.ReadAsStringAsync();
            return jsonResponse; // This will return the search results in JSON format
        } else {
            throw new Exception($"Error getting recent tracks: {response.StatusCode} - {response.ReasonPhrase}");
        }
    }


    
}