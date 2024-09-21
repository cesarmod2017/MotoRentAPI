using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MotoRent.Infrastructure.Data.Models
{
    public class DeliverymanModel
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("identifier")]
        public string Identifier { get; set; }

        [BsonElement("name")]
        public string Name { get; set; }

        [BsonElement("cnpj")]
        public string CNPJ { get; set; }

        [BsonElement("birth_date")]
        public DateTime BirthDate { get; set; }

        [BsonElement("license_number")]
        public string LicenseNumber { get; set; }

        [BsonElement("license_type")]
        public string LicenseType { get; set; }

        [BsonElement("license_image")]
        public string LicenseImage { get; set; }
    }
}