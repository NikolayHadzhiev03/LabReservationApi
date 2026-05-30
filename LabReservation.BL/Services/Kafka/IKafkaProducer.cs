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
        /// and publishes it as JSON to the configured topic.
        /// </summary>
        Task PublishAsync<TPayload>(
            string eventType,
            TPayload payload,
            CancellationToken cancellationToken = default)
            where TPayload : class;
    }
}
