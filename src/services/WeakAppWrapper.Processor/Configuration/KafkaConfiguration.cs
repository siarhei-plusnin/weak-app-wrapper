namespace WeakAppWrapper.Processor.Configuration;

public sealed class KafkaConfiguration
{
    public const string SectionName = "Kafka";

    public required string BootstrapServers { get; init; }
    public required string RawTopic { get; init; }
    public required string RawDeadLetterTopic { get; init; }
    public required string ProcessedTopic { get; init; }
    public required string ConsumerGroup { get; init; }
    public required string ConsumerName { get; init; }
    public required string ClientId { get; init; }
    public int WorkersCount { get; init; } = 1;
    public int BufferSize { get; init; } = 100;
}
