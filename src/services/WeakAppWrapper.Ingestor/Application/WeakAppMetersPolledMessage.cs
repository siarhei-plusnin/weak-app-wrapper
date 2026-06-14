using System.Text.Json;
using System.Text.Json.Serialization;

namespace WeakAppWrapper.Ingestor.Application;

public sealed record WeakAppMetersPolledMessage(
    [property: JsonPropertyName("messageId")] string MessageId,
    [property: JsonPropertyName("source")] string Source,
    [property: JsonPropertyName("fetchedAt")] DateTimeOffset FetchedAt,
    [property: JsonPropertyName("payload")] JsonElement Payload
);
