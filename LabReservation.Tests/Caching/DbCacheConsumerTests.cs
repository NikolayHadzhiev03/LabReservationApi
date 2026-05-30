using System.Text.Json;
using Confluent.Kafka;
using LabReservation.BL.Services.KafkaCache;
using LabReservation.Host.Services.KafkaCache;
using LabReservation.Models.Caching;
using LabReservation.Models.Configuration;
using LabReservation.Models.Entities;
using LabReservation.Models.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace LabReservation.Tests.Caching
{
    public class DbCacheConsumerTests
    {
        private static IOptions<KafkaSettings> CreateSettings() =>
            Options.Create(new KafkaSettings
            {
                BootstrapServers = "localhost:9092",
                CacheTopicName = "db-cache-topic",
                GroupId = "test-group"
            });

        private static (DbCacheConsumer consumer,
                        Mock<ICacheStore<Lab>> labCache,
                        Mock<ICacheStore<Reservation>> reservationCache,
                        Mock<ILogger<DbCacheConsumer>> logger)
            BuildConsumer()
        {
            var labCache = new Mock<ICacheStore<Lab>>();
            var reservationCache = new Mock<ICacheStore<Reservation>>();
            var logger = new Mock<ILogger<DbCacheConsumer>>();

            var consumer = new DbCacheConsumer(
                CreateSettings(),
                labCache.Object,
                reservationCache.Object,
                logger.Object,
                _ => Mock.Of<IConsumer<string, string>>());

            return (consumer, labCache, reservationCache, logger);
        }

        private static ConsumeResult<string, string> BuildConsumeResult(string eventType, object payload)
        {
            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            var envelope = new KafkaEventEnvelope
            {
                EventId = Guid.NewGuid().ToString(),
                EventType = eventType,
                Timestamp = DateTime.UtcNow,
                Payload = JsonSerializer.SerializeToElement(payload, options)
            };

            return new ConsumeResult<string, string>
            {
                Topic = "db-cache-topic",
                Partition = new Partition(0),
                Offset = new Offset(1),
                Message = new Message<string, string>
                {
                    Key = eventType,
                    Value = JsonSerializer.Serialize(envelope, options)
                }
            };
        }

        [Fact]
        public void ProcessMessage_LabsSnapshot_ReplacesLabCache()
        {
            var (consumer, labCache, reservationCache, _) = BuildConsumer();

            var snapshot = new LabsSnapshotEvent(
                new[]
                {
                    new Lab { Id = "1", Name = "Physics" },
                    new Lab { Id = "2", Name = "Chemistry" }
                },
                DateTime.UtcNow);

            consumer.ProcessMessage(BuildConsumeResult(KafkaEventTypes.LabsSnapshot, snapshot));

            labCache.Verify(c => c.ReplaceAll(It.Is<IEnumerable<Lab>>(labs => labs.Count() == 2)), Times.Once);
            reservationCache.Verify(c => c.ReplaceAll(It.IsAny<IEnumerable<Reservation>>()), Times.Never);
        }

        [Fact]
        public void ProcessMessage_ReservationsSnapshot_ReplacesReservationCache()
        {
            var (consumer, labCache, reservationCache, _) = BuildConsumer();

            var snapshot = new ReservationsSnapshotEvent(
                new[]
                {
                    new Reservation { Id = "r1", LabId = "1" }
                },
                DateTime.UtcNow);

            consumer.ProcessMessage(BuildConsumeResult(KafkaEventTypes.ReservationsSnapshot, snapshot));

            reservationCache.Verify(c => c.ReplaceAll(It.Is<IEnumerable<Reservation>>(r => r.Count() == 1)), Times.Once);
            labCache.Verify(c => c.ReplaceAll(It.IsAny<IEnumerable<Lab>>()), Times.Never);
        }

        [Fact]
        public void ProcessMessage_UnknownEventType_DoesNotTouchCaches_LogsWarning()
        {
            var (consumer, labCache, reservationCache, logger) = BuildConsumer();

            consumer.ProcessMessage(BuildConsumeResult("SomethingElse", new { foo = "bar" }));

            labCache.Verify(c => c.ReplaceAll(It.IsAny<IEnumerable<Lab>>()), Times.Never);
            reservationCache.Verify(c => c.ReplaceAll(It.IsAny<IEnumerable<Reservation>>()), Times.Never);
            logger.Verify(
                l => l.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public void ProcessMessage_MalformedJson_DoesNotTouchCaches_LogsWarning()
        {
            var (consumer, labCache, reservationCache, logger) = BuildConsumer();

            var consumeResult = new ConsumeResult<string, string>
            {
                Topic = "db-cache-topic",
                Partition = new Partition(0),
                Offset = new Offset(1),
                Message = new Message<string, string>
                {
                    Key = KafkaEventTypes.LabsSnapshot,
                    Value = "not valid json {{{"
                }
            };

            consumer.ProcessMessage(consumeResult);

            labCache.Verify(c => c.ReplaceAll(It.IsAny<IEnumerable<Lab>>()), Times.Never);
            reservationCache.Verify(c => c.ReplaceAll(It.IsAny<IEnumerable<Reservation>>()), Times.Never);
            logger.Verify(
                l => l.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<JsonException>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }
    }
}
