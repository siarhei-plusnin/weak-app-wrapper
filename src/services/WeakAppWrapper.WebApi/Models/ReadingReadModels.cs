using System.Text.Json;

namespace WeakAppWrapper.WebApi.Models;

public sealed record ReadingResponse(
    Guid Id,
    string Source,
    string Type,
    string Name,
    DateTimeOffset CapturedAt,
    DateTimeOffset CreatedAt,
    JsonElement Payload
)
{
    public string LocationName => Name;
}

public sealed record ReadingPageResponse(
    IReadOnlyList<ReadingResponse> Items,
    long TotalCount,
    int Page,
    int PageSize
);
