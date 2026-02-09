using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace LabReservation.Models.Entities
{
    /// <summary>
 /// Represents a reservation for a laboratory
    /// </summary>
    public class Reservation
  {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

    [BsonElement("labId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string LabId { get; set; } = string.Empty;

        [BsonElement("customerName")]
        public string CustomerName { get; set; } = string.Empty;

        [BsonElement("customerEmail")]
        public string CustomerEmail { get; set; } = string.Empty;

        [BsonElement("startTime")]
        public DateTime StartTime { get; set; }

        [BsonElement("endTime")]
        public DateTime EndTime { get; set; }

      [BsonElement("purpose")]
        public string Purpose { get; set; } = string.Empty;

     [BsonElement("status")]
        public ReservationStatus Status { get; set; } = ReservationStatus.Pending;

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("updatedAt")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
  }

    public enum ReservationStatus
    {
    Pending = 0,
        Confirmed = 1,
        Cancelled = 2,
    Completed = 3
    }
}
