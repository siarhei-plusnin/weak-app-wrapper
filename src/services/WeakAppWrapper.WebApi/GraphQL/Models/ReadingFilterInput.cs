using WeakAppWrapper.WebApi.Models;

namespace WeakAppWrapper.WebApi.GraphQL.Models;

public sealed class ReadingFilterInput
{
    public string? Source { get; init; }

    public string? Name { get; init; }

    public string? Type { get; init; }

    public MetricKey? MetricKey { get; init; }

    public DateTimeOffset? From { get; init; }

    public DateTimeOffset? To { get; init; }

    public double? MinValue { get; init; }

    public double? MaxValue { get; init; }
}
