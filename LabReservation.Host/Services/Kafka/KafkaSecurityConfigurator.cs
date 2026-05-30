using Confluent.Kafka;
using LabReservation.Models.Configuration;

namespace LabReservation.Host.Services.Kafka
{
    /// <summary>
    /// Applies optional SASL/SSL fields from <see cref="KafkaSettings"/>
    /// onto a Confluent.Kafka client config. No-op when SecurityProtocol is empty.
    /// </summary>
    internal static class KafkaSecurityConfigurator
    {
        public static void Apply(ClientConfig config, KafkaSettings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.SecurityProtocol))
            {
                return;
            }

            if (Enum.TryParse<SecurityProtocol>(settings.SecurityProtocol, ignoreCase: true, out var protocol))
            {
                config.SecurityProtocol = protocol;
            }

            if (!string.IsNullOrWhiteSpace(settings.SaslMechanism)
                && Enum.TryParse<SaslMechanism>(settings.SaslMechanism, ignoreCase: true, out var mechanism))
            {
                config.SaslMechanism = mechanism;
            }

            if (!string.IsNullOrWhiteSpace(settings.SaslUsername))
            {
                config.SaslUsername = settings.SaslUsername;
            }

            if (!string.IsNullOrWhiteSpace(settings.SaslPassword))
            {
                config.SaslPassword = settings.SaslPassword;
            }

            config.EnableSslCertificateVerification = settings.EnableSslCertificateVerification;
        }
    }
}
