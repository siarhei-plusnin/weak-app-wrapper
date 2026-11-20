using System.Text.Json;
using System.Text.Json.Serialization;

namespace WeakAppWrapper.Contracts.Messages;

public sealed record ProcessedReadingMessage(
    [property: JsonPropertyName("schemaVersion")] int SchemaVersion,
    [property: JsonPropertyName("messageId")] string MessageId,
    [property: JsonPropertyName("sourceMessageId")] string SourceMessageId,
    [property: JsonPropertyName("readingId")] Guid ReadingId,
    [property: JsonPropertyName("itemIndex")] int ItemIndex,
    [property: JsonPropertyName("source")] string Source,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("locationName")] string? LocationName,
    [property: JsonPropertyName("capturedAt")] DateTimeOffset CapturedAt,
    [property: JsonPropertyName("receivedAt")] DateTimeOffset ReceivedAt,
    [property: JsonPropertyName("processedAt")] DateTimeOffset ProcessedAt,
    [property: JsonPropertyName("payload")] JsonElement? Payload
);
