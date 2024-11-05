using MoodzApi.Models;
using MongoDB.Driver;
using Microsoft.Extensions.Options;

namespace MoodzApi.Services;

public class UsersService
{
    private readonly IMongoCollection<User> _usersCollection;
    public UsersService(
        IOptions<UserDatabaseSettings> userDatabaseSettings)
    {
        var mongoClient = new MongoClient(userDatabaseSettings.Value.ConnectionString);

        var mongoDatabase = mongoClient.GetDatabase(userDatabaseSettings.Value.DatabaseName);

        _usersCollection = mongoDatabase.GetCollection<User>(userDatabaseSettings.Value.UsersCollectionName);
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

    public async Task<UserEntries[]> GetEntriesByUserIdAsync(string id)
    {
        // Find the user by ID
        var user = await _usersCollection.Find(x => x.Id == id).FirstOrDefaultAsync();

        // If the user does not exist or has no entries, return an empty list
        return user?.Entries ?? Array.Empty<UserEntries>();
    }

    public async Task<bool> AddEntryToUserAsync(string id, UserEntries newEntry)
    {
        // Find the user and update the Entries array by adding the new entry
        var updateResult = await _usersCollection.UpdateOneAsync(
            x => x.Id == id,
            Builders<User>.Update.Push(x => x.Entries, newEntry)
        );

        // Return true if the update was successful, false otherwise
        return updateResult.ModifiedCount > 0;
    }

    public async Task<bool> DeleteEntryByDateAsync(string id, string date)
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

    //builder function if wanted
    /*
    public async Task<bool> AddUserCodeAsync(string userId, string code)
    {
        // Define the filter to locate the user by ID
        var filter = Builders<User>.Filter.Eq(x => x.Id, userId);

        // Define the update to set the Code field
        var update = Builders<User>.Update.Set(x => x.SpotifyAuthenticationCode, code);

        // Execute the update operation
        var result = await _usersCollection.UpdateOneAsync(filter, update);

        // Return true if the update was successful
        return result.ModifiedCount > 0;
    }*/

    public async Task<bool> AddUserAuthCodeAsync(string userId, string code)
    {
        // Find the user by ID
        var user = await _usersCollection.Find(x => x.Id == userId).FirstOrDefaultAsync();

        // If the user is not found, return false
        if (user == null)
            return false;

        // Update the Code field in the user object
        user.SpotifyAuthenticationCode = code;

        // Replace the existing document with the updated user document
        var result = await _usersCollection.ReplaceOneAsync(x => x.Id == userId, user);

        // Return true if the update was successful
        return result.IsAcknowledged && result.ModifiedCount > 0;
    }

    public async Task<bool> UpdateSpotifyAccessToken(string userId, SpotifyAccessToken token) {
        // Find the user by ID
        var user = await _usersCollection.Find(x => x.Id == userId).FirstOrDefaultAsync();

        // If the user is not found, return false
        if (user == null)
            return false;

        // Update the Code field in the user object
        user.SpotifyAccessToken = token;

        // adds refresh token string
        user.RefreshToken = token.RefreshToken;

        // Replace the existing document with the updated user document
        var result = await _usersCollection.ReplaceOneAsync(x => x.Id == userId, user);

        // Return true if the update was successful
        return result.IsAcknowledged && result.ModifiedCount > 0;
    }

    public async Task<string> GetSpotifyAuthorizationCode(string userId) {
        // Find the user by ID
        var user = await _usersCollection.Find(x => x.Id == userId).FirstOrDefaultAsync();
        return user.SpotifyAuthenticationCode;
    }

    public async Task<SpotifyAccessToken> GetSpotifyAccessToken(string userId) {
        // Find the user by ID
        var user = await _usersCollection.Find(x => x.Id == userId).FirstOrDefaultAsync();
        return user.SpotifyAccessToken;
    }

}