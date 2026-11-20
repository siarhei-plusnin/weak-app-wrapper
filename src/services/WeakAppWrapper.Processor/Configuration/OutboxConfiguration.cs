namespace WeakAppWrapper.Processor.Configuration;

public sealed class OutboxConfiguration
{
    public const string SectionName = "Outbox";

    public int PollIntervalSeconds { get; init; } = 2;
    public int BatchSize { get; init; } = 50;
}
