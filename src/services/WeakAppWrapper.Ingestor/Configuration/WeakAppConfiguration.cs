namespace WeakAppWrapper.Ingestor.Configuration;

public class WeakAppConfiguration
{
    public const string SectionName = "WeakApp";

    public required string BaseUrl { get; init; }
    public required string ApiKey { get; init; }
    public required int TimeoutSeconds { get; init; }
    public required WeakAppRetryConfiguration Retry { get; init; }
}

public class WeakAppRetryConfiguration
{
    public required int MaxRetries { get; init; }
    public required int DelaySeconds { get; init; }
}
