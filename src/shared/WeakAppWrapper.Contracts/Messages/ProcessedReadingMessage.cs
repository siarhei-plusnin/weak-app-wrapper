using System.Text.Json.Serialization;

namespace WeakAppWrapper.Contracts.Messages;

public sealed record ProcessedReadingMessage(
    [property: JsonPropertyName("messageId")] string MessageId,
    [property: JsonPropertyName("sourceMessageId")] string SourceMessageId,
    [property: JsonPropertyName("readingId")] Guid ReadingId,
    [property: JsonPropertyName("source")] string Source,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("observedAt")] DateTimeOffset ObservedAt,
    [property: JsonPropertyName("receivedAt")] DateTimeOffset ReceivedAt,
    [property: JsonPropertyName("processedAt")] DateTimeOffset ProcessedAt
);
