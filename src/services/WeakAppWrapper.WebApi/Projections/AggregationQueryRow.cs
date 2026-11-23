namespace WeakAppWrapper.WebApi.Projections;

public sealed class AggregationQueryRow
{
    public string Source { get; set; } = string.Empty;

    public string? Name { get; set; }

    public string? Type { get; set; }

    public DateTimeOffset? BucketStart { get; set; }

    public string? MetricKey { get; set; }

    public long Count { get; set; }

    public double? Sum { get; set; }

    public double? Average { get; set; }

    public double? Minimum { get; set; }

    public double? Maximum { get; set; }

    public long? TrueCount { get; set; }

    public long? FalseCount { get; set; }

    public double? TrueRate { get; set; }
}
