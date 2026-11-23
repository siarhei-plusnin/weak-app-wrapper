namespace WeakAppWrapper.WebApi.GraphQL.Models;

public abstract record ReadingPayload;

public sealed record AirQualityPayload(double? Co2, double? Pm25, double? Humidity) : ReadingPayload;

public sealed record EnergyPayload(double? Energy) : ReadingPayload;

public sealed record MotionPayload(bool? MotionDetected) : ReadingPayload;

public sealed record UnknownPayload(string Type, string Json) : ReadingPayload;

public sealed record GraphReading(
    Guid Id,
    string Source,
    string Type,
    string Name,
    string LocationName,
    DateTimeOffset CapturedAt,
    DateTimeOffset CreatedAt,
    ReadingPayload Payload
);

public sealed record ReadingPageGraph(IReadOnlyList<GraphReading> Items, long TotalCount, int Page, int PageSize);
