namespace LabReservation.BL.Services.Kafka
{
    /// <summary>
    /// Abstraction for publishing typed events to Kafka.
    /// BL services depend on this interface so they remain free of Confluent.Kafka concerns.
    /// </summary>
    public interface IKafkaProducer
    {
        /// <summary>
        /// Wraps <paramref name="payload"/> in a KafkaEventEnvelope (with EventId, EventType, Timestamp)
        /// and publishes it as JSON. When <paramref name="topic"/> is null the configured default
        /// topic is used; pass an explicit topic (e.g. the cache topic) to override it.
        /// </summary>
        Task PublishAsync<TPayload>(
            string eventType,
            TPayload payload,
            string? topic = null,
            CancellationToken cancellationToken = default)
            where TPayload : class;
    }
}
