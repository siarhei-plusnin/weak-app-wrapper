namespace WeakAppWrapper.WebApi.Models;

public enum ReadingSortField
{
    CapturedAt,
    Name,
    Type,
    Source,
}

public enum ReadingSortDirection
{
    Ascending,
    Descending,
}

public enum TimeBucket
{
    Minute,
    Hour,
    Day,
}

public sealed record ReadingFilter(
    string? Source,
    string? Name,
    string? Type,
    MetricKey? MetricKey,
    DateTimeOffset? From,
    DateTimeOffset? To,
    double? MinValue,
    double? MaxValue
)
{
    public static ReadingFilter Empty { get; } = new(null, null, null, null, null, null, null, null);
}

public sealed record ReadingQueryRequest(
    ReadingFilter Filter,
    int Page,
    int PageSize,
    ReadingSortField SortBy,
    ReadingSortDirection SortDirection
)
{
    public const int DefaultPageSize = 50;
    public const int MaximumPageSize = 100;
}
