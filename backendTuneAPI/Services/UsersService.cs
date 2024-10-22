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

    public async Task CreateAsync(User newBook) =>
        await _usersCollection.InsertOneAsync(newBook);

    public async Task UpdateAsync(string id, User updatedBook) =>
        await _usersCollection.ReplaceOneAsync(x => x.Id == id, updatedBook);

    public async Task RemoveAsync(string id) =>
        await _usersCollection.DeleteOneAsync(x => x.Id == id);
}