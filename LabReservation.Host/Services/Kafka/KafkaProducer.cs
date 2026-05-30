using System.Text.Json;
using Confluent.Kafka;
using LabReservation.BL.Services.Kafka;
using LabReservation.Models.Configuration;
using LabReservation.Models.Events;
using Microsoft.Extensions.Options;

namespace LabReservation.Host.Services.Kafka
{
    /// <summary>
    /// Confluent.Kafka-backed implementation of <see cref="IKafkaProducer"/>.
    /// Wraps each payload in a <see cref="KafkaEventEnvelope"/>, serializes to JSON,
    /// and produces to the configured topic using the EventType as the message key.
    /// </summary>
    public class KafkaProducer : IKafkaProducer, IDisposable
    {
        private readonly IProducer<string, string> _producer;
        private readonly KafkaSettings _settings;
        private readonly ILogger<KafkaProducer> _logger;
        private readonly JsonSerializerOptions _jsonOptions;
        private bool _disposed;

        public KafkaProducer(
            IOptions<KafkaSettings> settings,
            ILogger<KafkaProducer> logger,
            Func<ProducerConfig, IProducer<string, string>>? producerFactory = null)
        {
            _settings = settings.Value;
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };

            var config = new ProducerConfig
            {
                BootstrapServers = _settings.BootstrapServers,
                Acks = Acks.All,
                MessageSendMaxRetries = _settings.MessageMaxRetries,
                EnableIdempotence = true,
                MessageTimeoutMs = _settings.MessageSendTimeoutMs
            };

            KafkaSecurityConfigurator.Apply(config, _settings);

            var factory = producerFactory ?? (cfg => new ProducerBuilder<string, string>(cfg)
                .SetErrorHandler((_, error) => _logger.LogError(
                    "Kafka producer error: {Reason} (Code={Code}, IsFatal={IsFatal})",
                    error.Reason, error.Code, error.IsFatal))
                .Build());

            _producer = factory(config);

            _logger.LogInformation(
                "KafkaProducer initialized (BootstrapServers={Brokers}, Topic={Topic})",
                _settings.BootstrapServers, _settings.TopicName);
        }

        public async Task PublishAsync<TPayload>(
            string eventType,
            TPayload payload,
            CancellationToken cancellationToken = default)
            where TPayload : class
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
            ArgumentNullException.ThrowIfNull(payload);

            var payloadJson = JsonSerializer.SerializeToElement(payload, _jsonOptions);
            var envelope = new KafkaEventEnvelope
            {
                EventId = Guid.NewGuid().ToString(),
                EventType = eventType,
                Timestamp = DateTime.UtcNow,
                Payload = payloadJson
            };

            var envelopeJson = JsonSerializer.Serialize(envelope, _jsonOptions);

            try
            {
                var deliveryResult = await _producer.ProduceAsync(
                    _settings.TopicName,
                    new Message<string, string>
                    {
                        Key = eventType,
                        Value = envelopeJson
                    },
                    cancellationToken);

                _logger.LogInformation(
                    "Delivered {EventType} (EventId={EventId}) to {Topic} [partition {Partition}] @ offset {Offset}",
                    eventType,
                    envelope.EventId,
                    deliveryResult.Topic,
                    deliveryResult.Partition.Value,
                    deliveryResult.Offset.Value);
            }
            catch (ProduceException<string, string> ex)
            {
                _logger.LogError(ex,
                    "Failed to publish {EventType} to {Topic}: {Reason}",
                    eventType, _settings.TopicName, ex.Error.Reason);
                throw;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            try
            {
                _producer.Flush(TimeSpan.FromSeconds(5));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error flushing Kafka producer during dispose");
            }

            _producer.Dispose();
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
