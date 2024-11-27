using MoodzApi.Models;
using MongoDB.Driver;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using System.Collections.Generic;

namespace MoodzApi.Services;

public class UsersService
{
    private readonly IMongoDatabase _database;
    private readonly IMongoCollection<User> _usersCollection;
    private readonly IMongoCollection<CachedFeed> _feedsCollection;
    private readonly JwtSettings _jwtSettings;
    private readonly int _cachedFeedExpirationMinutes;
    public UsersService(
        IOptions<UserDatabaseSettings> userDatabaseSettings, IOptions<JwtSettings> jwtSettings)
    {
        var mongoClient = new MongoClient(userDatabaseSettings.Value.ConnectionString);
        _database = mongoClient.GetDatabase(userDatabaseSettings.Value.DatabaseName);
        _usersCollection = _database.GetCollection<User>(userDatabaseSettings.Value.UsersCollectionName);
        _feedsCollection = _database.GetCollection<CachedFeed>(userDatabaseSettings.Value.FeedsCollectionName);
        _cachedFeedExpirationMinutes = userDatabaseSettings.Value.CachedFeedExpirationMinutes;
        _jwtSettings = jwtSettings.Value;
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

        // Invalidate cached feed for the user
        InvalidateCacheFeed(ObjectId.Parse(id));

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

        // Invalidate cached feed for the user
        InvalidateCacheFeed(ObjectId.Parse(id));

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

    public async Task<bool> UpdateSettingsAsync(string id, Settings settings)
    {
        var user = await _usersCollection.Find(user => user.Id == id).FirstOrDefaultAsync();

        if (user == null) return false;

        user.Settings.Theme = settings.Theme ?? user.Settings.Theme;
        user.Settings.IsPrivate = settings.IsPrivate ?? user.Settings.IsPrivate;

        var result = await _usersCollection.ReplaceOneAsync(x => x.Id == id, user);

        return result.IsAcknowledged && result.ModifiedCount > 0;
    }

    public async Task<bool> UpdateUserRefreshToken(string userId, Auth refreshToken)
    {
        var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
        var update = Builders<User>.Update
            .Set(u => u.RefreshToken, refreshToken.RefreshToken)
            .Set(u => u.RefreshTokenExpiresAt, DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryDays));

        var result = await _usersCollection.UpdateOneAsync(filter, update);
        return result.IsAcknowledged && result.ModifiedCount > 0;
    }

    //check for refresh token string among the users in the database
    public async Task<User> GetUserWithRefreshToken(string refreshToken)
    {
        var user = await _usersCollection.Find(x => x.RefreshToken == refreshToken).FirstOrDefaultAsync();
        return user;
    }

    public async Task<bool> IsRefreshTokenExpired(string refreshToken)
    {
        var user = await GetUserWithRefreshToken(refreshToken);

        // If user not found, or refresh token doesn't exist, or refresh token is expired, return true
        if (user == null || user.RefreshTokenExpiresAt == null || user.RefreshTokenExpiresAt <= DateTime.UtcNow)
        {
            return true;
        }

        return false;
    }

    public async Task<bool> Follow(ObjectId fromUserId, ObjectId toUserId)
    {
        var toUser = await GetAsync(toUserId.ToString());
        // If toUser doesn't exist or already following them, return false
        if (toUser == null || toUser.Followers.Contains(fromUserId)) return false;
        // If a follow request has already been sent, return false
        if (toUser.FollowRequests?.Any(fr => fr.FromUserId == fromUserId && fr.ToUserId == toUserId) ?? false) return false;

        var toUserIsPrivate = toUser.Settings?.IsPrivate ?? false;
        var matchFromUser = Builders<User>.Filter.Eq(u => u.Id, fromUserId.ToString());
        var matchToUser = Builders<User>.Filter.Eq(u => u.Id, toUserId.ToString());

        // using ensures that the session object is disposed of after we are done using it.
        using var session = await _database.Client.StartSessionAsync();
        session.StartTransaction();
        try
        {
            if (toUserIsPrivate)
            {
                var request = new FollowRequest() { FromUserId = fromUserId, ToUserId = toUserId, Status = Status.Pending };
                var pushRequest = Builders<User>.Update.Push(u => u.FollowRequests, request);

                // Run both tasks simultaneously 
                var task1 = _usersCollection.UpdateOneAsync(matchToUser, pushRequest);
                var task2 = _usersCollection.UpdateOneAsync(matchFromUser, pushRequest);
                await Task.WhenAll(task1, task2);   // Wait for both to finish before returning

                // If both tasks succeeded, then commit both changes to the database.
                // This ensures that if an error occurs, there will be no data discrepencies (ex: toUser has a follow request, but fromUser doesn't have a sent follow request)
                await session.CommitTransactionAsync();

                return task1.Result.IsAcknowledged && task2.Result.IsAcknowledged && task1.Result.ModifiedCount > 0 && task2.Result.ModifiedCount > 0;
            }
            else
            {
                var pushFollowing = Builders<User>.Update.Push(u => u.Following, toUserId);
                var pushFollower = Builders<User>.Update.Push(u => u.Followers, fromUserId);

                // Run both tasks simultaneously 
                var task1 = _usersCollection.UpdateOneAsync(matchFromUser, pushFollowing);
                var task2 = _usersCollection.UpdateOneAsync(matchToUser, pushFollower);
                await Task.WhenAll(task1, task2);   // Wait for both to finish before returning

                // If both tasks succeeded, then commit both changes to the database.
                // This ensures that if an error occurs, there will be no data discrepencies (ex: fromUser is following toUser, but toUser has no fromUser follower)
                await session.CommitTransactionAsync();

                // Invalidate cached feed for fromUserId since they now have +1 following
                InvalidateCacheFeed(fromUserId);

                return task1.Result.IsAcknowledged && task2.Result.IsAcknowledged && task1.Result.ModifiedCount > 0 && task2.Result.ModifiedCount > 0;
            }
        }
        catch (Exception e)
        {
            await session.AbortTransactionAsync();
            Console.WriteLine($"Follow Transaction aborted: {e}");
            return false;
        }
    }

    public async Task<bool> Unfollow(ObjectId fromUserId, ObjectId toUserId)
    {
        var toUser = await GetAsync(toUserId.ToString());
        // If toUser doesn't exist or not following them, return false
        if (toUser == null || !toUser.Followers.Contains(fromUserId)) return false;

        var matchFromUser = Builders<User>.Filter.Eq(u => u.Id, fromUserId.ToString());
        var matchToUser = Builders<User>.Filter.Eq(u => u.Id, toUserId.ToString());

        var removeFollowing = Builders<User>.Update.Pull(u => u.Following, toUserId);
        var removeFollower = Builders<User>.Update.Pull(u => u.Followers, fromUserId);

        using var session = await _database.Client.StartSessionAsync();
        session.StartTransaction();
        try
        {
            // Run both tasks simultaneously 
            var task1 = _usersCollection.UpdateOneAsync(matchFromUser, removeFollowing);
            var task2 = _usersCollection.UpdateOneAsync(matchToUser, removeFollower);
            await Task.WhenAll(task1, task2);   // Wait for both to finish before returning

            // If both tasks succeeded, then commit both changes to the database.
            // This ensures that if an error occurs, there will be no data discrepencies
            await session.CommitTransactionAsync();

            // Invalidate cached feed for fromUserId since they now have -1 following
            InvalidateCacheFeed(fromUserId);

            return task1.Result.IsAcknowledged && task2.Result.IsAcknowledged && task1.Result.ModifiedCount > 0 && task2.Result.ModifiedCount > 0;
        }
        catch (Exception e)
        {
            await session.AbortTransactionAsync();
            Console.WriteLine($"Unfollow Transaction aborted: {e}");
            return false;
        }
    }

    public async Task<bool> AcceptFollowRequest(ObjectId fromUserId, ObjectId toUserId)
    {
        var matchFromUser = Builders<User>.Filter.Eq(u => u.Id, fromUserId.ToString());
        var matchToUser = Builders<User>.Filter.Eq(u => u.Id, toUserId.ToString());

        // Filter to match the follow request
        var matchRequest = Builders<FollowRequest>.Filter.And(
            Builders<FollowRequest>.Filter.Eq("FromUserId", fromUserId),
            Builders<FollowRequest>.Filter.Eq("ToUserId", toUserId),
            Builders<FollowRequest>.Filter.Eq("Status", Status.Pending)
        );
        // Remove the follow request from the FollowRequests array
        var removeRequest = Builders<User>.Update.PullFilter(u => u.FollowRequests, matchRequest);

        // Add Follower/Following to each user
        var addFollower = removeRequest.Push(u => u.Followers, fromUserId);
        var addFollowing = removeRequest.Push(u => u.Following, toUserId);

        using var session = await _database.Client.StartSessionAsync();
        session.StartTransaction();
        try
        {
            // Run both tasks simultaenously
            var task1 = _usersCollection.UpdateOneAsync(matchToUser, addFollower);
            var task2 = _usersCollection.UpdateOneAsync(matchFromUser, addFollowing);
            await Task.WhenAll(task1, task2);   // Wait for both to finish before returning

            // If both tasks succeeded, then commit both changes to the database.
            // This ensures that if an error occurs, there will be no data discrepencies
            await session.CommitTransactionAsync();

            // Invalidate cached feed for fromUserId since they now have +1 following
            InvalidateCacheFeed(fromUserId);

            return task1.Result.IsAcknowledged && task2.Result.IsAcknowledged && task1.Result.ModifiedCount > 0 && task2.Result.ModifiedCount > 0;
        }
        catch (Exception e)
        {
            await session.AbortTransactionAsync();
            Console.WriteLine($"Accept Follow Request Transaction aborted: {e}");
            return false;
        }
    }

    public async Task<bool> RemoveFollowRequest(ObjectId fromUserId, ObjectId toUserId)
    {
        var matchFromUser = Builders<User>.Filter.Eq(u => u.Id, fromUserId.ToString());
        var matchToUser = Builders<User>.Filter.Eq(u => u.Id, toUserId.ToString());

        // Filter to match the follow request
        var matchRequest = Builders<FollowRequest>.Filter.And(
            Builders<FollowRequest>.Filter.Eq("FromUserId", fromUserId),
            Builders<FollowRequest>.Filter.Eq("ToUserId", toUserId)
        );
        // Remove the follow request from the FollowRequests array
        var removeRequest = Builders<User>.Update.PullFilter(u => u.FollowRequests, matchRequest);

        using var session = await _database.Client.StartSessionAsync();
        session.StartTransaction();
        try
        {
            // Run both tasks simultaenously
            var task1 = _usersCollection.UpdateOneAsync(matchToUser, removeRequest);
            var task2 = _usersCollection.UpdateOneAsync(matchFromUser, removeRequest);
            await Task.WhenAll(task1, task2);   // Wait for both to finish before returning

            // If both tasks succeeded, then commit both changes to the database.
            // This ensures that if an error occurs, there will be no data discrepencies
            await session.CommitTransactionAsync();

            return task1.Result.IsAcknowledged && task2.Result.IsAcknowledged && task1.Result.ModifiedCount > 0 && task2.Result.ModifiedCount > 0;
        }
        catch (Exception e)
        {
            await session.AbortTransactionAsync();
            Console.WriteLine($"Decline Follow Request Transaction aborted: {e}");
            return false;
        }
    }

    public async Task<List<FeedEntry>> GetFeedAsync(ObjectId currentUserId, int limit = 50)
    {
        // Check if cache of feed exists, if null is returned proceed to generate a new feed
        var cachedFeed = await GetCachedFeed(currentUserId);
        if (cachedFeed != null) return cachedFeed;

        var pipeline = new[]
        {
            // Match the current user
            new BsonDocument("$match", new BsonDocument("_id", currentUserId)),

            // Add the user's own _id to the Following array to include user's own entries in feed
            new BsonDocument("$addFields", new BsonDocument("Following",
                new BsonDocument("$concatArrays", new BsonArray {
                    "$Following", new BsonArray { currentUserId }
                })
            )),

            // Unwind the "following" array, basically spreads out the following array so that only one entry is in each document
            new BsonDocument("$unwind", "$Following"),

            // Match the followed users with their user documents
            new BsonDocument("$lookup", new BsonDocument
            {
                { "from", "users" },  // Join with the "users" collection
                { "localField", "Following" },  // following field should just be the followed user's ObjectId after unwinding
                { "foreignField", "_id" },  // Match the followed user's ObjectId with the ObjectId of their user document
                { "as", "FollowedUser" }    // Output the user document as followerUser field
            }),

            new BsonDocument("$unwind", "$FollowedUser"), // Unwind FollowedUser to be a field instead of array
            new BsonDocument("$unwind", "$FollowedUser.Entries"), // Unwind each user's entries

            new BsonDocument("$unset", "_id"),   // Remove original id

            // Extract only the fields needed for the feed
            new BsonDocument("$project", new BsonDocument
            {
                { "_id", "$FollowedUser._id" }, // Replace with entry owner id
                { "Name", "$FollowedUser.Name" },
                { "ProfilePicUrl", "$FollowedUser.ProfilePicUrl" },
                { "Text", "$FollowedUser.Entries.Text" },
                { "Likes", "$FollowedUser.Entries.Likes" },
                { "Track", "$FollowedUser.Entries.Track" },
                { "Date", "$FollowedUser.Entries.Date" }
            }),

            // Sort the entries by date in descending order
            new BsonDocument("$sort", new BsonDocument("Date", -1)),

            // Limit the result to the most recent N entries
            new BsonDocument("$limit", limit)
        };

        // Run the aggregation pipeline
        var result = await _usersCollection.Aggregate<FeedEntry>(pipeline).ToListAsync();

        var newCachedFeed = new CachedFeed()
        {
            Id = currentUserId,
            Expiration = DateTime.UtcNow.AddMinutes(_cachedFeedExpirationMinutes),
            Feed = result.Select(entry => new FeedEntry
            {
                Id = entry.Id,
                ProfilePicUrl = entry.ProfilePicUrl,
                Name = entry.Name,
                Text = entry.Text,
                Likes = entry.Likes,
                Track = entry.Track,
                Date = entry.Date
            }).ToList()
        };

        // Store new cached feed 
        await _feedsCollection.ReplaceOneAsync(x => x.Id == currentUserId, newCachedFeed, new ReplaceOptions { IsUpsert = true });

        return result;
    }

    private async Task<List<FeedEntry>?> GetCachedFeed(ObjectId id)
    {
        var cachedFeed = await _feedsCollection.Find<CachedFeed>(x => x.Id == id).FirstOrDefaultAsync();

        // if cachedFeed doesn't exist or if expired, return null
        if (cachedFeed == null || cachedFeed.Expiration < DateTime.UtcNow)
        {
            return null;
        }

        return cachedFeed.Feed;
    }

    // Invalidate cached feed for user
    private async void InvalidateCacheFeed(ObjectId id)
    {
        await _feedsCollection.DeleteOneAsync(x => x.Id == id);
    }
}