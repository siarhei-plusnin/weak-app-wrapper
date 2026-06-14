namespace WeakAppWrapper.Ingestor.Configuration;

public class IngestorConfiguration
{
    public const string SectionName = "Ingestor";

    public required int PollIntervalSeconds { get; init; }
}
