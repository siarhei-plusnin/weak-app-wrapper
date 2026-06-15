using System.Text.Json;

namespace WeakAppWrapper.Processor.Application;

public sealed record NormalizedReading(
    string Source,
    string Type,
    string Name,
    JsonElement Payload,
    DateTimeOffset ObservedAt,
    DateTimeOffset ReceivedAt
);
