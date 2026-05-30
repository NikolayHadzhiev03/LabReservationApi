using System.Text.Json;
using Confluent.Kafka;
using LabReservation.BL.Services.Interfaces;
using LabReservation.Host.Services.Kafka;
using LabReservation.Models.Configuration;
using LabReservation.Models.DTO;
using LabReservation.Models.Entities;
using LabReservation.Models.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace LabReservation.Tests.Kafka
{
    public class KafkaConsumerTests
    {
        private static IOptions<KafkaSettings> CreateSettings() =>
            Options.Create(new KafkaSettings
            {
                BootstrapServers = "localhost:9092",
                TopicName = "messages-topic",
                GroupId = "test-group"
            });

        private static (KafkaConsumer consumer,
                        Mock<IReservationService> reservationService,
                        Mock<ILabService> labService,
                        Mock<ILogger<KafkaConsumer>> logger)
            BuildConsumer()
        {
            var reservationService = new Mock<IReservationService>();
            var labService = new Mock<ILabService>();

            var services = new ServiceCollection();
            services.AddScoped(_ => reservationService.Object);
            services.AddScoped(_ => labService.Object);
            var sp = services.BuildServiceProvider();
            var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();

            var logger = new Mock<ILogger<KafkaConsumer>>();
            var consumer = new KafkaConsumer(
                CreateSettings(),
                scopeFactory,
                logger.Object,
                _ => Mock.Of<IConsumer<string, string>>());

            return (consumer, reservationService, labService, logger);
        }

        private static ConsumeResult<string, string> BuildConsumeResult(string eventType, object payload)
        {
            var envelope = new KafkaEventEnvelope
            {
                EventId = Guid.NewGuid().ToString(),
                EventType = eventType,
                Timestamp = DateTime.UtcNow,
                Payload = JsonSerializer.SerializeToElement(payload, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                })
            };

            return new ConsumeResult<string, string>
            {
                Topic = "messages-topic",
                Partition = new Partition(0),
                Offset = new Offset(1),
                Message = new Message<string, string>
                {
                    Key = eventType,
                    Value = JsonSerializer.Serialize(envelope, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    })
                }
            };
        }

        [Fact]
        public async Task ProcessMessage_ReservationCreated_InvokesReservationService()
        {
            var (consumer, reservationService, _, _) = BuildConsumer();
            reservationService
                .Setup(s => s.GetByIdAsync("res-1"))
                .ReturnsAsync(new ReservationDto { Id = "res-1", LabId = "lab-1" });

            var consumeResult = BuildConsumeResult(
                KafkaEventTypes.ReservationCreated,
                new ReservationCreatedEvent(
                    "res-1", "lab-1", "Alice", "alice@example.com",
                    DateTime.UtcNow, DateTime.UtcNow.AddHours(1),
                    "Research", ReservationStatus.Pending, DateTime.UtcNow));

            await consumer.ProcessMessageAsync(consumeResult, CancellationToken.None);

            reservationService.Verify(s => s.GetByIdAsync("res-1"), Times.Once);
        }

        [Fact]
        public async Task ProcessMessage_LabCreated_InvokesLabService()
        {
            var (consumer, _, labService, _) = BuildConsumer();
            labService
                .Setup(s => s.GetByIdAsync("lab-1"))
                .ReturnsAsync(new LabDto { Id = "lab-1", Name = "Physics" });

            var consumeResult = BuildConsumeResult(
                KafkaEventTypes.LabCreated,
                new LabCreatedEvent(
                    "lab-1", "Physics", "Building A", 30,
                    new[] { "Microscope" }, true, DateTime.UtcNow));

            await consumer.ProcessMessageAsync(consumeResult, CancellationToken.None);

            labService.Verify(s => s.GetByIdAsync("lab-1"), Times.Once);
        }

        [Fact]
        public async Task ProcessMessage_UnknownEventType_LogsWarning_DoesNotThrow()
        {
            var (consumer, reservationService, labService, logger) = BuildConsumer();

            var consumeResult = BuildConsumeResult(
                "SomethingWeNeverEmit",
                new { foo = "bar" });

            await consumer.ProcessMessageAsync(consumeResult, CancellationToken.None);

            reservationService.Verify(s => s.GetByIdAsync(It.IsAny<string>()), Times.Never);
            labService.Verify(s => s.GetByIdAsync(It.IsAny<string>()), Times.Never);

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
        public async Task ProcessMessage_MalformedJson_LogsWarning_DoesNotThrow()
        {
            var (consumer, reservationService, labService, logger) = BuildConsumer();

            var consumeResult = new ConsumeResult<string, string>
            {
                Topic = "messages-topic",
                Partition = new Partition(0),
                Offset = new Offset(1),
                Message = new Message<string, string>
                {
                    Key = KafkaEventTypes.LabCreated,
                    Value = "this is not valid json {{{"
                }
            };

            await consumer.ProcessMessageAsync(consumeResult, CancellationToken.None);

            reservationService.Verify(s => s.GetByIdAsync(It.IsAny<string>()), Times.Never);
            labService.Verify(s => s.GetByIdAsync(It.IsAny<string>()), Times.Never);
            logger.Verify(
                l => l.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<JsonException>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public void Constructor_AcceptsMockedDependencies()
        {
            var (consumer, _, _, _) = BuildConsumer();
            Assert.NotNull(consumer);
        }
    }
}
