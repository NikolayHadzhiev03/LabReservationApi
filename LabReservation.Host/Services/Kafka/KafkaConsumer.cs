using System.Text.Json;
using Confluent.Kafka;
using LabReservation.BL.Services.Interfaces;
using LabReservation.Models.Configuration;
using LabReservation.Models.Events;
using Microsoft.Extensions.Options;

namespace LabReservation.Host.Services.Kafka
{
    /// <summary>
    /// Background service that subscribes to the configured Kafka topic,
    /// deserializes each message into a <see cref="KafkaEventEnvelope"/>,
    /// and dispatches it to the appropriate BL service inside a fresh DI scope.
    /// </summary>
    public class KafkaConsumer : BackgroundService
    {
        private readonly KafkaSettings _settings;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<KafkaConsumer> _logger;
        private readonly Func<ConsumerConfig, IConsumer<string, string>> _consumerFactory;
        private readonly JsonSerializerOptions _jsonOptions;
        private IConsumer<string, string>? _consumer;

        public KafkaConsumer(
            IOptions<KafkaSettings> settings,
            IServiceScopeFactory scopeFactory,
            ILogger<KafkaConsumer> logger,
            Func<ConsumerConfig, IConsumer<string, string>>? consumerFactory = null)
        {
            _settings = settings.Value;
            _scopeFactory = scopeFactory;
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };

            _consumerFactory = consumerFactory ?? (cfg => new ConsumerBuilder<string, string>(cfg)
                .SetErrorHandler((_, error) => _logger.LogError(
                    "Kafka consumer error: {Reason} (Code={Code}, IsFatal={IsFatal})",
                    error.Reason, error.Code, error.IsFatal))
                .Build());
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var config = new ConsumerConfig
            {
                BootstrapServers = _settings.BootstrapServers,
                GroupId = _settings.GroupId,
                AutoOffsetReset = Enum.TryParse<AutoOffsetReset>(_settings.AutoOffsetReset, true, out var aor)
                    ? aor
                    : AutoOffsetReset.Earliest,
                EnableAutoCommit = false,
                AllowAutoCreateTopics = true
            };

            KafkaSecurityConfigurator.Apply(config, _settings);

            _consumer = _consumerFactory(config);
            _consumer.Subscribe(_settings.TopicName);

            _logger.LogInformation(
                "KafkaConsumer subscribed to {Topic} (GroupId={GroupId}, Brokers={Brokers})",
                _settings.TopicName, _settings.GroupId, _settings.BootstrapServers);

            // Yield once so host startup completes before we enter the blocking consume loop.
            await Task.Yield();

            while (!stoppingToken.IsCancellationRequested)
            {
                ConsumeResult<string, string>? consumeResult = null;
                try
                {
                    consumeResult = _consumer.Consume(stoppingToken);
                    if (consumeResult?.Message == null)
                    {
                        continue;
                    }

                    await ProcessMessageAsync(consumeResult, stoppingToken);
                    _consumer.Commit(consumeResult);
                }
                catch (OperationCanceledException)
                {
                    // Clean shutdown path.
                    break;
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex,
                        "Kafka consume error: {Reason} (Code={Code})",
                        ex.Error.Reason, ex.Error.Code);

                    // Short backoff before next attempt to avoid a tight error loop while the driver reconnects.
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
                catch (Exception ex)
                {
                    // Per-message failure: log and skip so a single poison message can't kill the loop.
                    _logger.LogError(ex,
                        "Unhandled error processing Kafka message at offset {Offset} on partition {Partition}",
                        consumeResult?.Offset.Value, consumeResult?.Partition.Value);

                    if (consumeResult != null)
                    {
                        try { _consumer.Commit(consumeResult); }
                        catch (Exception commitEx)
                        {
                            _logger.LogWarning(commitEx, "Failed to commit offset after poison message");
                        }
                    }
                }
            }
        }

        internal async Task ProcessMessageAsync(
            ConsumeResult<string, string> consumeResult,
            CancellationToken cancellationToken)
        {
            KafkaEventEnvelope? envelope;
            try
            {
                envelope = JsonSerializer.Deserialize<KafkaEventEnvelope>(
                    consumeResult.Message.Value,
                    _jsonOptions);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex,
                    "Skipping malformed Kafka message at offset {Offset} (Key={Key})",
                    consumeResult.Offset.Value, consumeResult.Message.Key);
                return;
            }

            if (envelope == null || string.IsNullOrWhiteSpace(envelope.EventType))
            {
                _logger.LogWarning(
                    "Skipping Kafka message with missing envelope or EventType at offset {Offset}",
                    consumeResult.Offset.Value);
                return;
            }

            _logger.LogInformation(
                "Received event {EventType} (EventId={EventId}, Timestamp={Timestamp}) from {Topic}",
                envelope.EventType, envelope.EventId, envelope.Timestamp, consumeResult.Topic);

            await DispatchAsync(envelope, cancellationToken);
        }

        private async Task DispatchAsync(KafkaEventEnvelope envelope, CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();

            switch (envelope.EventType)
            {
                case KafkaEventTypes.LabCreated:
                case KafkaEventTypes.LabUpdated:
                {
                    var payload = envelope.Payload.Deserialize<LabCreatedEvent>(_jsonOptions);
                    if (payload != null)
                    {
                        var labService = scope.ServiceProvider.GetRequiredService<ILabService>();
                        var lab = await labService.GetByIdAsync(payload.LabId);
                        _logger.LogInformation(
                            "Processed {EventType} for lab {LabId} (existsInStore={Exists}, name={Name})",
                            envelope.EventType, payload.LabId, lab != null, payload.Name);
                    }
                    break;
                }

                case KafkaEventTypes.LabDeleted:
                {
                    var payload = envelope.Payload.Deserialize<LabDeletedEvent>(_jsonOptions);
                    _logger.LogInformation(
                        "Processed {EventType} for lab {LabId} (deletedAt={DeletedAt})",
                        envelope.EventType, payload?.LabId, payload?.DeletedAt);
                    break;
                }

                case KafkaEventTypes.ReservationCreated:
                case KafkaEventTypes.ReservationUpdated:
                {
                    var payload = envelope.Payload.Deserialize<ReservationCreatedEvent>(_jsonOptions);
                    if (payload != null)
                    {
                        var reservationService = scope.ServiceProvider.GetRequiredService<IReservationService>();
                        var reservation = await reservationService.GetByIdAsync(payload.ReservationId);
                        _logger.LogInformation(
                            "Processed {EventType} for reservation {ReservationId} (existsInStore={Exists}, lab={LabId})",
                            envelope.EventType, payload.ReservationId, reservation != null, payload.LabId);
                    }
                    break;
                }

                case KafkaEventTypes.ReservationCancelled:
                case KafkaEventTypes.ReservationConfirmed:
                {
                    var payload = envelope.Payload.Deserialize<ReservationCancelledEvent>(_jsonOptions);
                    if (payload != null)
                    {
                        var reservationService = scope.ServiceProvider.GetRequiredService<IReservationService>();
                        var reservation = await reservationService.GetByIdAsync(payload.ReservationId);
                        _logger.LogInformation(
                            "Processed {EventType} for reservation {ReservationId} (existsInStore={Exists}, customer={Email})",
                            envelope.EventType, payload.ReservationId, reservation != null, payload.CustomerEmail);
                    }
                    break;
                }

                case KafkaEventTypes.ReservationDeleted:
                {
                    var payload = envelope.Payload.Deserialize<ReservationDeletedEvent>(_jsonOptions);
                    _logger.LogInformation(
                        "Processed {EventType} for reservation {ReservationId} (deletedAt={DeletedAt})",
                        envelope.EventType, payload?.ReservationId, payload?.DeletedAt);
                    break;
                }

                default:
                    _logger.LogWarning(
                        "Unknown EventType {EventType} (EventId={EventId}) — message skipped",
                        envelope.EventType, envelope.EventId);
                    break;
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Closing Kafka consumer");
            try
            {
                _consumer?.Close();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error closing Kafka consumer");
            }
            await base.StopAsync(cancellationToken);
        }

        public override void Dispose()
        {
            _consumer?.Dispose();
            base.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
