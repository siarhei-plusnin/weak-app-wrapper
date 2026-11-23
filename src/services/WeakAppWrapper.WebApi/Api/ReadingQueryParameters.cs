using Microsoft.AspNetCore.Mvc;

namespace WeakAppWrapper.WebApi.Api;

public sealed class ReadingQueryParameters
{
    [FromQuery(Name = "source")]
    public string? Source { get; set; }

    [FromQuery(Name = "name")]
    public string? Name { get; set; }

    [FromQuery(Name = "type")]
    public string? Type { get; set; }

    [FromQuery(Name = "metricKey")]
    public string? MetricKey { get; set; }

    [FromQuery(Name = "from")]
    public DateTimeOffset? From { get; set; }

    [FromQuery(Name = "to")]
    public DateTimeOffset? To { get; set; }

    [FromQuery(Name = "minValue")]
    public double? MinValue { get; set; }

    [FromQuery(Name = "maxValue")]
    public double? MaxValue { get; set; }

    [FromQuery(Name = "page")]
    public int? Page { get; set; }

    [FromQuery(Name = "pageSize")]
    public int? PageSize { get; set; }

    [FromQuery(Name = "sortBy")]
    public string? SortBy { get; set; }

    [FromQuery(Name = "sortDirection")]
    public string? SortDirection { get; set; }

    [FromQuery(Name = "bucket")]
    public string? Bucket { get; set; }
}
