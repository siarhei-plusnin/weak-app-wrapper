using System.Text.Json;

namespace WeakAppWrapper.Processor.Application;

public sealed record NormalizedReading(
    string Source,
    string SourceMessageId,
    int ItemIndex,
    string Type,
    string Name,
    JsonElement Payload,
    JsonElement RawRecord,
    DateTimeOffset CapturedAt
);
