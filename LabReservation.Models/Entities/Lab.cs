using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace LabReservation.Models.Entities
{
    /// <summary>
    /// Represents a laboratory in the system
    /// </summary>
    public class Lab
    {
  [BsonId]
  [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

     [BsonElement("name")]
     public string Name { get; set; } = string.Empty;

        [BsonElement("location")]
    public string Location { get; set; } = string.Empty;

        [BsonElement("capacity")]
        public int Capacity { get; set; }

        [BsonElement("equipment")]
        public List<string> Equipment { get; set; } = new();

     [BsonElement("isAvailable")]
        public bool IsAvailable { get; set; } = true;

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

   [BsonElement("updatedAt")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
}
