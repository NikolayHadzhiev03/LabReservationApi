using System.Text.Json;
using Confluent.Kafka;
using LabReservation.Host.Services.Kafka;
using LabReservation.Models.Configuration;
using LabReservation.Models.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace LabReservation.Tests.Kafka
{
    public class KafkaProducerTests
    {
        private static IOptions<KafkaSettings> CreateSettings() =>
            Options.Create(new KafkaSettings
            {
                BootstrapServers = "localhost:9092",
                TopicName = "messages-topic",
                GroupId = "test-group"
            });

        [Fact]
        public async Task PublishAsync_WrapsPayloadInEnvelope_AndProducesToTopic()
        {
            // Arrange
            Message<string, string>? capturedMessage = null;
            string? capturedTopic = null;

            var innerProducer = new Mock<IProducer<string, string>>();
            innerProducer
                .Setup(p => p.ProduceAsync(
                    It.IsAny<string>(),
                    It.IsAny<Message<string, string>>(),
                    It.IsAny<CancellationToken>()))
                .Callback<string, Message<string, string>, CancellationToken>((topic, msg, _) =>
                {
                    capturedTopic = topic;
                    capturedMessage = msg;
                })
                .ReturnsAsync(() => new DeliveryResult<string, string>
                {
                    Topic = capturedTopic!,
                    Partition = new Partition(0),
                    Offset = new Offset(42),
                    Message = capturedMessage!
                });

            var producer = new KafkaProducer(
                CreateSettings(),
                Mock.Of<ILogger<KafkaProducer>>(),
                _ => innerProducer.Object);

            var payload = new LabCreatedEvent(
                "lab-1", "Physics Lab", "Building A", 30,
                new[] { "Microscope" }, true, DateTime.UtcNow);

            // Act
            await producer.PublishAsync(KafkaEventTypes.LabCreated, payload);

            // Assert
            Assert.Equal("messages-topic", capturedTopic);
            Assert.NotNull(capturedMessage);
            Assert.Equal(KafkaEventTypes.LabCreated, capturedMessage!.Key);

            var envelope = JsonSerializer.Deserialize<KafkaEventEnvelope>(
                capturedMessage.Value,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.NotNull(envelope);
            Assert.Equal(KafkaEventTypes.LabCreated, envelope!.EventType);
            Assert.False(string.IsNullOrWhiteSpace(envelope.EventId));
            Assert.True((DateTime.UtcNow - envelope.Timestamp).TotalSeconds < 5);

            var deserializedPayload = envelope.Payload.Deserialize<LabCreatedEvent>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Assert.Equal("lab-1", deserializedPayload!.LabId);
            Assert.Equal("Physics Lab", deserializedPayload.Name);
        }

        [Fact]
        public async Task PublishAsync_PropagatesProduceException_AndLogsError()
        {
            // Arrange
            var brokerError = new Error(ErrorCode.BrokerNotAvailable, "Broker not available");
            var innerProducer = new Mock<IProducer<string, string>>();
            innerProducer
                .Setup(p => p.ProduceAsync(
                    It.IsAny<string>(),
                    It.IsAny<Message<string, string>>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ProduceException<string, string>(
                    brokerError,
                    new DeliveryResult<string, string> { Message = new Message<string, string>() }));

            var logger = new Mock<ILogger<KafkaProducer>>();
            var producer = new KafkaProducer(
                CreateSettings(),
                logger.Object,
                _ => innerProducer.Object);

            // Act + Assert
            await Assert.ThrowsAsync<ProduceException<string, string>>(() =>
                producer.PublishAsync(KafkaEventTypes.LabCreated, new LabDeletedEvent("lab-1", DateTime.UtcNow)));

            logger.Verify(
                l => l.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<ProduceException<string, string>>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task PublishAsync_RejectsNullOrEmptyEventType()
        {
            var innerProducer = new Mock<IProducer<string, string>>();
            var producer = new KafkaProducer(
                CreateSettings(),
                Mock.Of<ILogger<KafkaProducer>>(),
                _ => innerProducer.Object);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                producer.PublishAsync(string.Empty, new LabDeletedEvent("lab-1", DateTime.UtcNow)));
        }

        [Fact]
        public async Task PublishAsync_RejectsNullPayload()
        {
            var innerProducer = new Mock<IProducer<string, string>>();
            var producer = new KafkaProducer(
                CreateSettings(),
                Mock.Of<ILogger<KafkaProducer>>(),
                _ => innerProducer.Object);

            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                producer.PublishAsync<LabDeletedEvent>(KafkaEventTypes.LabDeleted, null!));
        }

        [Fact]
        public void Dispose_FlushesAndDisposesInnerProducer()
        {
            var innerProducer = new Mock<IProducer<string, string>>();
            var producer = new KafkaProducer(
                CreateSettings(),
                Mock.Of<ILogger<KafkaProducer>>(),
                _ => innerProducer.Object);

            producer.Dispose();

            innerProducer.Verify(p => p.Flush(It.IsAny<TimeSpan>()), Times.Once);
            innerProducer.Verify(p => p.Dispose(), Times.Once);
        }
    }
}
