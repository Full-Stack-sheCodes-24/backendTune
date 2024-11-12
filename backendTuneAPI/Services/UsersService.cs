using MoodzApi.Models;
using MongoDB.Driver;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Xml.Linq;
using MongoDB.Bson;
using System.Security.Cryptography;

namespace MoodzApi.Services;

public class UsersService
{
    private readonly IMongoCollection<User> _usersCollection;
    private readonly JwtSettings _jwtSettings;
    private readonly JwtTokenService _jwtTokenService;
    public UsersService(
        IOptions<UserDatabaseSettings> userDatabaseSettings, IOptions<JwtSettings> jwtSettings, JwtTokenService jwtTokenService)
    {
        var mongoClient = new MongoClient(userDatabaseSettings.Value.ConnectionString);
        var mongoDatabase = mongoClient.GetDatabase(userDatabaseSettings.Value.DatabaseName);
        _usersCollection = mongoDatabase.GetCollection<User>(userDatabaseSettings.Value.UsersCollectionName);
        _jwtSettings = jwtSettings.Value;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<List<User>> GetAsync() =>
        await _usersCollection.Find(_ => true).ToListAsync();

    public async Task<User?> GetAsync(string id) =>
        await _usersCollection.Find(x => x.Id == id).FirstOrDefaultAsync();

    public async Task<User?> CreateAsync(User newUser)
    {
        await _usersCollection.InsertOneAsync(newUser); // Id is generated by MongoDB and populates the Id field in newUser
        return newUser;
    }

    public async Task<bool> UpdateAsync(User updatedUser)
    {
        var result = await _usersCollection.ReplaceOneAsync(x => x.Id == updatedUser.Id, updatedUser);
        return result.IsAcknowledged;
    }

    public async Task<bool> RemoveAsync(string id)
    {
        var result = await _usersCollection.DeleteOneAsync(x => x.Id == id);
        return result.IsAcknowledged;
    }

    public async Task<List<Entry>> GetEntriesByUserIdAsync(string id)
    {
        // Find the user by ID
        var user = await _usersCollection.Find(x => x.Id == id).FirstOrDefaultAsync();

        // If the user does not exist or has no entries, return an empty list
        return user?.Entries ?? new List<Entry>();
    }

    public async Task<bool> AddEntryToUserAsync(string id, Entry newEntry)
    {
        var user = await _usersCollection.Find(x => x.Id == id).FirstOrDefaultAsync();
        if (user.Entries == null)
        {
            user.Entries = new List<Entry>(); // Initialize as an empty list if null
        }

        user.Entries.Add(newEntry);

        // Find the user and update the Entries array by adding the new entry
        var updateResult = await _usersCollection.ReplaceOneAsync(
            x => x.Id == id, user);

        // Return true if the update was successful, false otherwise
        return updateResult.ModifiedCount > 0;
    }

    public async Task<bool> DeleteEntryByDateAsync(string id, DateTime date)
    {
        // Define the filter to match the specific entry by date string
        var filter = Builders<User>.Filter.And(
            Builders<User>.Filter.Eq(x => x.Id, id),
            Builders<User>.Filter.ElemMatch(x => x.Entries, entry => entry.Date == date)
        );

        // Define the update to remove the entry with the specified date
        var update = Builders<User>.Update.PullFilter(x => x.Entries, entry => entry.Date == date);

        // Perform the update
        var updateResult = await _usersCollection.UpdateOneAsync(filter, update);

        // Return true if an entry was deleted, false otherwise
        return updateResult.ModifiedCount > 0;
    }

    public async Task<bool> UpdateSpotifyAccessToken(string userId, SpotifyUserAccessToken token) {
        // Find the user by ID
        var user = await _usersCollection.Find(x => x.Id == userId).FirstOrDefaultAsync();

        // If the user is not found, return false
        if (user == null)
            return false;

        // Update the Code field in the user object
        user.SpotifyUserAccessToken = token;

        // Replace the existing document with the updated user document
        var result = await _usersCollection.ReplaceOneAsync(x => x.Id == userId, user);

        // Return true if the update was successful
        return result.IsAcknowledged && result.ModifiedCount > 0;
    }

    public async Task<SpotifyUserAccessToken> GetSpotifyUserAccessToken(string userId) {
        // Find the user by ID
        var user = await _usersCollection.Find(x => x.Id == userId).FirstOrDefaultAsync();
        return user.SpotifyUserAccessToken;
    }

    public async Task<User> GetUserWithSpotifyId(string spotifyId)
    {
        // Find the user by ID
        var user = await _usersCollection.Find(x => x.SpotifyId == spotifyId).FirstOrDefaultAsync();

        return user;
    }

    public async Task<List<User>> SearchUsersByName(string query)
    {
        var users = new List<User>();

        // Filter for names that start with the search term (prefix match, case-insensitive)
        var prefixMatchFilter = Builders<User>.Filter.Regex("Name", new BsonRegularExpression($"^{query}", "i"));

        // Filter for names that contain the search term anywhere (partial match, case-insensitive)
        var partialMatchFilter = Builders<User>.Filter.Regex("Name", new BsonRegularExpression(query, "i"));

        // First, retrieve users where the name starts with the search term, sorted alphabetically
        var prefixMatches = await _usersCollection
            .Find(prefixMatchFilter)
            .Sort(Builders<User>.Sort.Ascending("Name"))
            .Limit(10)
            .ToListAsync();

        users.AddRange(prefixMatches);

        // If we still need more results to reach a limit of 10, fetch partial matches excluding prefix matches
        if (users.Count < 10)
        {
            // Gather the IDs of the prefix matches to exclude them from the next query
            var excludedIds = prefixMatches.Select(u => u.Id).ToList();

            // Create a filter that excludes users already in prefixMatches
            var excludePrefixMatchesFilter = Builders<User>.Filter.And(
                partialMatchFilter,
                Builders<User>.Filter.Nin(u => u.Id, excludedIds) // Ensuring IDs are excluded properly
            );

            var partialMatches = await _usersCollection
                .Find(excludePrefixMatchesFilter)
                .Sort(Builders<User>.Sort.Ascending("Name"))
                .Limit(10 - users.Count) // Limit by remaining slots
                .ToListAsync();

            users.AddRange(partialMatches);
        }

        return users;
    }

    public async Task<bool> UpdateProfileInfoAsync(string id, ProfileInfo profileInfo)
    {
        var user = await _usersCollection.Find(user => user.Id == id).FirstOrDefaultAsync();

        if (user == null) return false;

        user.ProfilePicUrl = profileInfo.ProfilePicUrl ?? user.ProfilePicUrl;
        user.BioText = profileInfo.BioText ?? user.BioText;
        user.Birthday = profileInfo.Birthday ?? user.Birthday;

        var result = await _usersCollection.ReplaceOneAsync(x => x.Id == id, user);

        return result.IsAcknowledged && result.ModifiedCount > 0;
    }

    public async Task<bool> UpdateUserRefreshToken(string userId, Auth refreshToken)
    {
        var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
        var update = Builders<User>.Update
            .Set(u => u.RefreshToken, refreshToken.RefreshToken)
            .Set(u => u.RefreshTokenExpiresAt, DateTime.Now.AddMinutes(refreshToken.ExpiresIn));

        var result = await _usersCollection.UpdateOneAsync(filter, update);
        return result.IsAcknowledged && result.ModifiedCount > 0;
    }

    //generate the refresh token for the user with corresponding id
    public async Task<Auth> GenerateNewAuth(string userId)
    {
        var refreshTokenString = _jwtTokenService.GenerateRefreshToken();

        var newAuth = new Auth
        {
            AccessToken = _jwtTokenService.GenerateToken(userId),
            RefreshToken = refreshTokenString,
            ExpiresIn = _jwtSettings.ExpiryMinutes
        };

        await UpdateUserRefreshToken(userId, newAuth);

        return newAuth;     
    }

    //check for refresh token string among the users in the database
    public async Task<User> GetUserWithRefreshToken(string refreshToken)
    {
        var user = await _usersCollection.Find(x => x.RefreshToken == refreshToken).FirstOrDefaultAsync();
        return user;
    }

    //check for refresh token string among the users in the database
    public async Task<string> ValidateRefreshToken(string refreshToken)
    {
        var user = await GetUserWithRefreshToken(refreshToken);
        if (user == null || user.RefreshTokenExpiresAt == null || user.RefreshTokenExpiresAt <= DateTime.UtcNow)
        {
            return null;
        }

        return user.Id;
    }
}