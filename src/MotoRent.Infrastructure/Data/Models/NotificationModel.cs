using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MotoRent.Infrastructure.Data.Models
{
    public class NotificationModel
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("message")]
        public string Message { get; set; }

        [BsonElement("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}