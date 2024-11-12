using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MoodzApi.Mappers;
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
    private readonly string _redirectUri;
    private readonly UserMapper _userMapper;
    private readonly SpotifyMapper _spotifyMapper;
    private readonly JwtTokenService _jwtTokenService;
    private readonly JwtSettings _jwtSettings;

    public SpotifyService(IOptions<SpotifyAuthSettings> spotifyAuthSettings, UsersService usersService, 
        JwtTokenService jwtTokenService, IOptions<JwtSettings> jwtSettings, IConfiguration configuration)
    {
        _httpClient = new HttpClient();
        _clientId = spotifyAuthSettings.Value.ClientId;
        _clientSecret = spotifyAuthSettings.Value.ClientSecret;
        _accessToken = new SpotifyAccessToken();
        _usersService = usersService;
        _redirectUri = configuration["Spotify:RedirectUri"]!;
        _userMapper = new UserMapper();
        _spotifyMapper = new SpotifyMapper();
        _jwtTokenService = jwtTokenService;
        _jwtSettings = jwtSettings.Value;
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

    // User access token swapping
    private async Task<SpotifyUserAccessToken> GetUserAccessToken(string authCode)
    {
        var authHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_clientId}:{_clientSecret}"));
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authHeader);

        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "authorization_code"),
            new KeyValuePair<string, string>("code", authCode),
            new KeyValuePair<string, string>("redirect_uri", _redirectUri)
        });

        HttpResponseMessage response = default!;

        try {
            response = await _httpClient.PostAsync("https://accounts.spotify.com/api/token", content);
        }
        catch (Exception ex) {
            Console.WriteLine(ex.Message);
            Console.WriteLine(ex.InnerException?.Message);  // May reveal more details about the SSL issue
        }

        if (response.IsSuccessStatusCode) {
            var jsonResponse = await response.Content.ReadAsStringAsync();
            var tokenData = JsonConvert.DeserializeObject<SpotifyUserAccessToken>(jsonResponse);
            tokenData!.SetExpiration();
            
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
    private async Task<SpotifyUserAccessToken> CheckUserAccessToken(string userId)
    {
        SpotifyUserAccessToken userToken = await _usersService.GetSpotifyUserAccessToken(userId);

        if (userToken.IsExpired())
        {
            userToken = await RefreshAccessToken(userId, userToken);
        }

        return userToken;
    }

    // Called when userToken is expired, returns a new access token
    private async Task<SpotifyUserAccessToken> RefreshAccessToken(string userId, SpotifyUserAccessToken userToken)
    {
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
            var tokenData = JsonConvert.DeserializeObject<SpotifyUserAccessToken>(jsonResponse);
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
    public async Task<string> GetMostRecentTracks(string userId)
    {
        SpotifyAccessToken userAccessToken = await CheckUserAccessToken(userId);
        
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

    // Login user and return user state
    public async Task<UserState> SpotifyUserLogin(string authCode)
    {
        SpotifyUserAccessToken userAccessToken = await GetUserAccessToken(authCode);

        // Use accessToken to get spotifyId
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.spotify.com/v1/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userAccessToken.AccessToken);
        var response = await _httpClient.SendAsync(request);

        if (response.IsSuccessStatusCode)
        {
            var jsonResponse = await response.Content.ReadAsStringAsync();
            SpotifyUser spotifyUser = JsonConvert.DeserializeObject<SpotifyUser>(jsonResponse)!;

            // Get user based on their SpotifyId
            User user = await _usersService.GetUserWithSpotifyId(spotifyUser.Id);

            // If new user, register them
            if (user == null) {
                User newUser = _spotifyMapper.SpotifyUserToUser(spotifyUser);
                user = (await _usersService.CreateAsync(newUser))!;
            }

            // Store access token in user doc
            await _usersService.UpdateSpotifyAccessToken(user.Id!, userAccessToken);

            // Condense user information down to just what we want to expose the frontend to
            UserState userState = _userMapper.UserToUserState(user);

            //update userState with jwt and refresh tokens to the frontend
            var jwtToken = _jwtTokenService.GenerateToken(user.Id);
            var refreshToken = await _jwtTokenService.GenerateRefreshToken(user.Id);

            userState.JwtToken.accessToken = jwtToken;
            userState.RefreshToken = refreshToken;
            userState.RefreshToken.expiresAt = refreshToken.expiresAt;

            return userState;
        }
        else
        {
            throw new Exception($"Error logging in user: {response.StatusCode} - {response.ReasonPhrase}");
        }
    }

    public async Task<string> SearchTracksWithUserAccessToken(string query, string userId)
    {
        SpotifyAccessToken userAccessToken = await CheckUserAccessToken(userId);

        // Encode the query to handle special characters
        var encodedQuery = Uri.EscapeDataString(query);
        var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.spotify.com/v1/search?q={encodedQuery}&type=track&market=US&include_external=audio");

        // Add the Authorization header to the request
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userAccessToken.AccessToken);

        var response = await _httpClient.SendAsync(request);

        if (response.IsSuccessStatusCode)
        {
            var jsonResponse = await response.Content.ReadAsStringAsync();
            return jsonResponse; // This will return the search results in JSON format
        }
        else
        {
            throw new Exception($"Error searching tracks: {response.StatusCode} - {response.ReasonPhrase}");
        }
    }

    // get track info
    public async Task<string> GetTrack(string trackId, string userId)
    {
        SpotifyAccessToken userAccessToken = await CheckUserAccessToken(userId);

        var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.spotify.com/v1/tracks/{trackId}?market=US");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userAccessToken.AccessToken);

        var response = await _httpClient.SendAsync(request);

        if (response.IsSuccessStatusCode)
        {
            var jsonResponse = await response.Content.ReadAsStringAsync();
            return jsonResponse; // This will return the search results in JSON format
        }
        else
        {
            throw new Exception($"Error getting track info: {response.StatusCode} - {response.ReasonPhrase}");
        }
    }
}
