using System.Text.Json;
using Confluent.Kafka;
using LabReservation.BL.Services.KafkaCache;
using LabReservation.Host.Services.Kafka;
using LabReservation.Models.Caching;
using LabReservation.Models.Configuration;
using LabReservation.Models.Entities;
using LabReservation.Models.Events;
using Microsoft.Extensions.Options;

namespace LabReservation.Host.Services.KafkaCache
{
    /// <summary>
    /// Background service that subscribes to the dedicated cache topic and applies each
    /// full-collection snapshot to the matching in-memory <see cref="ICacheStore{T}"/>.
    /// Mirrors <see cref="KafkaConsumer"/> (manual commit, poison-message skip, graceful shutdown)
    /// but uses its own consumer group so it reads the cache topic independently.
    /// </summary>
    public class DbCacheConsumer : BackgroundService
    {
        private readonly KafkaSettings _settings;
        private readonly ICacheStore<Lab> _labCache;
        private readonly ICacheStore<Reservation> _reservationCache;
        private readonly ILogger<DbCacheConsumer> _logger;
        private readonly Func<ConsumerConfig, IConsumer<string, string>> _consumerFactory;
        private readonly JsonSerializerOptions _jsonOptions;
        private IConsumer<string, string>? _consumer;

        public DbCacheConsumer(
            IOptions<KafkaSettings> settings,
            ICacheStore<Lab> labCache,
            ICacheStore<Reservation> reservationCache,
            ILogger<DbCacheConsumer> logger,
            Func<ConsumerConfig, IConsumer<string, string>>? consumerFactory = null)
        {
            _settings = settings.Value;
            _labCache = labCache;
            _reservationCache = reservationCache;
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };

            _consumerFactory = consumerFactory ?? (cfg => new ConsumerBuilder<string, string>(cfg)
                .SetErrorHandler((_, error) => _logger.LogError(
                    "Cache consumer error: {Reason} (Code={Code}, IsFatal={IsFatal})",
                    error.Reason, error.Code, error.IsFatal))
                .Build());
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var config = new ConsumerConfig
            {
                BootstrapServers = _settings.BootstrapServers,
                // Separate group so the cache consumer reads the cache topic independently of the event consumer.
                GroupId = $"{_settings.GroupId}-cache",
                AutoOffsetReset = Enum.TryParse<AutoOffsetReset>(_settings.AutoOffsetReset, true, out var aor)
                    ? aor
                    : AutoOffsetReset.Earliest,
                EnableAutoCommit = false,
                AllowAutoCreateTopics = true
            };

            KafkaSecurityConfigurator.Apply(config, _settings);

            _consumer = _consumerFactory(config);
            _consumer.Subscribe(_settings.CacheTopicName);

            _logger.LogInformation(
                "DbCacheConsumer subscribed to {Topic} (GroupId={GroupId}, Brokers={Brokers})",
                _settings.CacheTopicName, config.GroupId, _settings.BootstrapServers);

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

                    ProcessMessage(consumeResult);
                    _consumer.Commit(consumeResult);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex,
                        "Cache consume error: {Reason} (Code={Code})",
                        ex.Error.Reason, ex.Error.Code);

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
                    _logger.LogError(ex,
                        "Unhandled error processing cache message at offset {Offset} on partition {Partition}",
                        consumeResult?.Offset.Value, consumeResult?.Partition.Value);

                    if (consumeResult != null)
                    {
                        try { _consumer.Commit(consumeResult); }
                        catch (Exception commitEx)
                        {
                            _logger.LogWarning(commitEx, "Failed to commit offset after poison cache message");
                        }
                    }
                }
            }
        }

        internal void ProcessMessage(ConsumeResult<string, string> consumeResult)
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
                    "Skipping malformed cache message at offset {Offset} (Key={Key})",
                    consumeResult.Offset.Value, consumeResult.Message.Key);
                return;
            }

            if (envelope == null || string.IsNullOrWhiteSpace(envelope.EventType))
            {
                _logger.LogWarning(
                    "Skipping cache message with missing envelope or EventType at offset {Offset}",
                    consumeResult.Offset.Value);
                return;
            }

            switch (envelope.EventType)
            {
                case KafkaEventTypes.LabsSnapshot:
                {
                    var snapshot = envelope.Payload.Deserialize<LabsSnapshotEvent>(_jsonOptions);
                    if (snapshot != null)
                    {
                        _labCache.ReplaceAll(snapshot.Labs);
                        _logger.LogInformation(
                            "Applied labs snapshot to cache ({Count} labs, snapshotAt={SnapshotAt})",
                            snapshot.Labs.Count, snapshot.SnapshotAt);
                    }
                    break;
                }

                case KafkaEventTypes.ReservationsSnapshot:
                {
                    var snapshot = envelope.Payload.Deserialize<ReservationsSnapshotEvent>(_jsonOptions);
                    if (snapshot != null)
                    {
                        _reservationCache.ReplaceAll(snapshot.Reservations);
                        _logger.LogInformation(
                            "Applied reservations snapshot to cache ({Count} reservations, snapshotAt={SnapshotAt})",
                            snapshot.Reservations.Count, snapshot.SnapshotAt);
                    }
                    break;
                }

                default:
                    _logger.LogWarning(
                        "Unknown cache EventType {EventType} (EventId={EventId}) — message skipped",
                        envelope.EventType, envelope.EventId);
                    break;
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Closing cache consumer");
            try
            {
                _consumer?.Close();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error closing cache consumer");
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
