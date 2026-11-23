namespace WeakAppWrapper.WebApi.Models;

public sealed record LatestMetricValueResponse(
    Guid ReadingId,
    string Source,
    string Name,
    string Type,
    string MetricKey,
    DateTimeOffset CapturedAt,
    double? NumericValue,
    bool? BooleanValue
)
{
    public string LocationName => Name;
}
