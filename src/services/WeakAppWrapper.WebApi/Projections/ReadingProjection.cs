namespace WeakAppWrapper.WebApi.Projections;

public sealed record ReadingProjection(
    Guid Id,
    string Source,
    string Type,
    string Name,
    string RawRecord,
    DateTimeOffset CapturedAt,
    DateTimeOffset CreatedAt
);
