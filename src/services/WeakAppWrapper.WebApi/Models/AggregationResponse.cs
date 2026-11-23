namespace WeakAppWrapper.WebApi.Models;

public sealed record AggregationResponse(
    string Source,
    string? Name,
    string? Type,
    DateTimeOffset? BucketStart,
    string? MetricKey,
    long Count,
    double? Sum,
    double? Average,
    double? Minimum,
    double? Maximum,
    long? TrueCount,
    long? FalseCount,
    double? TrueRate
)
{
    public string? LocationName => Name;
}
