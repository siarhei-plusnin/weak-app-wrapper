using System.Text.Json;
using System.Text.Json.Serialization;

namespace WeakAppWrapper.Contracts.Messages;

public sealed record RawMessageDeadLetterMessage(
    [property: JsonPropertyName("messageId")] string MessageId,
    [property: JsonPropertyName("sourceMessageId")] string SourceMessageId,
    [property: JsonPropertyName("source")] string Source,
    [property: JsonPropertyName("failedAt")] DateTimeOffset FailedAt,
    [property: JsonPropertyName("errorType")] string ErrorType,
    [property: JsonPropertyName("errorMessage")] string ErrorMessage,
    [property: JsonPropertyName("rawMessage")] JsonElement RawMessage
);
