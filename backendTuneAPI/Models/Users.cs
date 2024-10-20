using MongoDB.Bson;

namespace backendTuneAPI.Models
{
    public class Users
    {
        public ObjectId Id { get; set; }
        public string email { get; set; }
    }
}
