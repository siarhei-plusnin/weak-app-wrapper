namespace WeakAppWrapper.Ingestor.Configuration;

public class KafkaConfiguration
{
    public const string SectionName = "Kafka";

    public required string BootstrapServers { get; init; }
    public required string Topic { get; init; }
    public required string ClientId { get; init; }
}
