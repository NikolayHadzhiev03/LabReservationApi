using System.Text.Json;

namespace LabReservation.Models.Events
{
    /// <summary>
    /// Generic envelope for every event published to the Kafka topic.
    /// EventType is the discriminator the consumer routes on; Payload holds the typed event data as JSON.
    /// </summary>
    public record KafkaEventEnvelope
    {
        public string EventId { get; init; } = Guid.NewGuid().ToString();
        public string EventType { get; init; } = string.Empty;
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        public JsonElement Payload { get; init; }
    }

    /// <summary>
    /// Well-known event type discriminators.
    /// </summary>
    public static class KafkaEventTypes
    {
        public const string LabCreated = "LabCreated";
        public const string LabUpdated = "LabUpdated";
        public const string LabDeleted = "LabDeleted";
        public const string ReservationCreated = "ReservationCreated";
        public const string ReservationUpdated = "ReservationUpdated";
        public const string ReservationDeleted = "ReservationDeleted";
        public const string ReservationCancelled = "ReservationCancelled";
        public const string ReservationConfirmed = "ReservationConfirmed";

        // Periodic full-collection snapshots published by DbCacheReader to the cache topic.
        public const string LabsSnapshot = "LabsSnapshot";
        public const string ReservationsSnapshot = "ReservationsSnapshot";
    }
}
