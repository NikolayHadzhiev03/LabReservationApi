using LabReservation.BL.Services.Kafka;
using LabReservation.DataLayer.Repositories.Interfaces;
using LabReservation.Models.Caching;
using LabReservation.Models.Configuration;
using LabReservation.Models.Events;
using Microsoft.Extensions.Options;

namespace LabReservation.Host.Services.DbCacheReader
{
    /// <summary>
    /// Background service that reads the whole labs and reservations collections at startup,
    /// then re-reads them on a configurable interval, publishing each as a full snapshot to the
    /// dedicated cache topic. Consumers apply these snapshots to their in-memory KafkaCache.
    /// </summary>
    public class DbCacheReaderService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IKafkaProducer _producer;
        private readonly KafkaSettings _settings;
        private readonly ILogger<DbCacheReaderService> _logger;

        public DbCacheReaderService(
            IServiceScopeFactory scopeFactory,
            IKafkaProducer producer,
            IOptions<KafkaSettings> settings,
            ILogger<DbCacheReaderService> logger)
        {
            _scopeFactory = scopeFactory;
            _producer = producer;
            _settings = settings.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var intervalSeconds = _settings.CacheRefreshIntervalSeconds > 0
                ? _settings.CacheRefreshIntervalSeconds
                : 60;

            _logger.LogInformation(
                "DbCacheReader starting (CacheTopic={Topic}, IntervalSeconds={Interval})",
                _settings.CacheTopicName, intervalSeconds);

            // Yield so host startup completes before the first (potentially slow) DB read.
            await Task.Yield();

            // Initial snapshot at startup.
            await PublishSnapshotsAsync(stoppingToken);

            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(intervalSeconds));
            try
            {
                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    await PublishSnapshotsAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Clean shutdown.
            }
        }

        private async Task PublishSnapshotsAsync(CancellationToken cancellationToken)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var labRepository = scope.ServiceProvider.GetRequiredService<ILabRepository>();
                var reservationRepository = scope.ServiceProvider.GetRequiredService<IReservationRepository>();

                var labs = (await labRepository.GetAllAsync()).ToList();
                var reservations = (await reservationRepository.GetAllAsync()).ToList();

                var snapshotAt = DateTime.UtcNow;

                await _producer.PublishAsync(
                    KafkaEventTypes.LabsSnapshot,
                    new LabsSnapshotEvent(labs, snapshotAt),
                    _settings.CacheTopicName,
                    cancellationToken);

                await _producer.PublishAsync(
                    KafkaEventTypes.ReservationsSnapshot,
                    new ReservationsSnapshotEvent(reservations, snapshotAt),
                    _settings.CacheTopicName,
                    cancellationToken);

                _logger.LogInformation(
                    "Published DB snapshot to {Topic} ({LabCount} labs, {ReservationCount} reservations)",
                    _settings.CacheTopicName, labs.Count, reservations.Count);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                // A failed snapshot must not crash the reader; the next tick will retry.
                _logger.LogError(ex, "Failed to read and publish DB snapshot to {Topic}", _settings.CacheTopicName);
            }
        }
    }
}
