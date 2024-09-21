using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MotoRent.Infrastructure.Data.Models
{
    public class MotorcycleModel
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("identifier")]
        public string Identifier { get; set; }

        [BsonElement("year")]
        public int Year { get; set; }

        [BsonElement("model")]
        public string Model { get; set; }

        [BsonElement("license_plate")]
        public string LicensePlate { get; set; }
    }
}