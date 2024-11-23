namespace MoodzApi.Models;

public class UserDatabaseSettings
{
    public string ConnectionString { get; set; } = null!;
    public string DatabaseName { get; set; } = null!;
    public string UsersCollectionName { get; set; } = null!;
    public string FeedsCollectionName { get; set; } = null!;
    public int CachedFeedExpirationMinutes { get; set; } = 5;
}