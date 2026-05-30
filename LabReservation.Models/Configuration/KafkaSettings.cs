namespace LabReservation.Models.Configuration
{
    /// <summary>
    /// Kafka connection and topic settings for the IOptions pattern.
    /// SASL/SSL fields are optional and only applied when SecurityProtocol is set.
    /// </summary>
    public class KafkaSettings
    {
        public string BootstrapServers { get; set; } = "localhost:9092";
        public string TopicName { get; set; } = "messages-topic";

        // Dedicated topic for periodic full-collection DB snapshots (DbCacheReader -> KafkaCache).
        public string CacheTopicName { get; set; } = "db-cache-topic";

        // How often DbCacheReader re-reads the database and republishes a snapshot.
        public int CacheRefreshIntervalSeconds { get; set; } = 60;

        public string GroupId { get; set; } = "lab-reservation-consumer-group";
        public string AutoOffsetReset { get; set; } = "Earliest";
        public int MessageMaxRetries { get; set; } = 3;
        public int MessageSendTimeoutMs { get; set; } = 5000;

        // Optional security settings (leave SecurityProtocol empty/null for plaintext local broker).
        // Examples: "SaslSsl", "SaslPlaintext", "Ssl", "Plaintext".
        public string? SecurityProtocol { get; set; }

        // Examples: "ScramSha256", "ScramSha512", "Plain".
        public string? SaslMechanism { get; set; }

        public string? SaslUsername { get; set; }
        public string? SaslPassword { get; set; }

        public bool EnableSslCertificateVerification { get; set; } = true;
    }
}
