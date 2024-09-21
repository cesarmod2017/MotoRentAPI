using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MotoRent.Infrastructure.Data.Models
{
    public class RentalModel
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("identifier")]
        public string Identifier { get; set; }

        [BsonElement("daily_rate")]
        public decimal DailyRate { get; set; }

        [BsonElement("deliveryman_id")]
        public string DeliverymanId { get; set; }

        [BsonElement("motorcycle_id")]
        public string MotorcycleId { get; set; }

        [BsonElement("start_date")]
        public DateTime StartDate { get; set; }

        [BsonElement("end_date")]
        public DateTime EndDate { get; set; }

        [BsonElement("expected_end_date")]
        public DateTime ExpectedEndDate { get; set; }

        [BsonElement("return_date")]
        public DateTime? ReturnDate { get; set; }

        [BsonElement("plan")]
        public int Plan { get; set; }

        [BsonElement("total_cost")]
        public decimal? TotalCost { get; set; }
    }
}