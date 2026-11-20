using System.Text.Json;
using WeakAppWrapper.Contracts.Messages;

namespace WeakAppWrapper.Processor.Application;

public static class WeakAppReadingNormalizer
{
    private const string DefaultSource = "weakapp";
    private const int MaxSourceLength = 100;
    private const int MaxSourceMessageIdLength = 200;
    private const int MaxTypeLength = 100;
    private const int MaxNameLength = 200;

    public static IReadOnlyList<NormalizedReading> Normalize(WeakAppMetersPolledMessage message)
    {
        string messageId = ReadRequiredString(
            message.MessageId,
            "WeakApp poll messageId must be a non-empty string",
            MaxSourceMessageIdLength
        );

        if (message.FetchedAt == default)
        {
            throw new WeakAppPayloadException("WeakApp poll is missing a valid fetchedAt timestamp");
        }

        string source = string.IsNullOrWhiteSpace(message.Source) ? DefaultSource : message.Source.Trim();
        if (source.Length > MaxSourceLength)
        {
            throw new WeakAppPayloadException("WeakApp source exceeds the maximum supported length");
        }

        if (message.Payload.ValueKind != JsonValueKind.Array)
        {
            throw new WeakAppPayloadException("WeakApp payload root must be a JSON array of meters");
        }

        return message
            .Payload.EnumerateArray()
            .Select((meter, itemIndex) =>
            {
                if (meter.ValueKind != JsonValueKind.Object)
                {
                    throw new WeakAppPayloadException("WeakApp meter entries must be JSON objects");
                }

                return new NormalizedReading(
                    source,
                    messageId,
                    itemIndex,
                    ReadRequiredString(meter, "type", "WeakApp meter type must be a non-empty string", MaxTypeLength),
                    ReadRequiredString(meter, "name", "WeakApp meter name must be a non-empty string", MaxNameLength),
                    ReadPayloadObject(meter).Clone(),
                    meter.Clone(),
                    message.FetchedAt
                );
            })
            .ToArray();
    }

    private static JsonElement ReadPayloadObject(JsonElement meter)
    {
        if (!meter.TryGetProperty("payload", out JsonElement payload) || payload.ValueKind != JsonValueKind.Object)
        {
            throw new WeakAppPayloadException("WeakApp meter payload must be a JSON object");
        }

        return payload;
    }

    private static string ReadRequiredString(JsonElement element, string propertyName, string errorMessage, int maxLength)
    {
        if (!element.TryGetProperty(propertyName, out JsonElement property) || property.ValueKind != JsonValueKind.String)
        {
            throw new WeakAppPayloadException(errorMessage);
        }

        return ReadRequiredString(property.GetString(), errorMessage, maxLength);
    }

    private static string ReadRequiredString(string? value, string errorMessage, int maxLength)
    {
        string normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length == 0)
        {
            throw new WeakAppPayloadException(errorMessage);
        }

        if (normalized.Length > maxLength)
        {
            throw new WeakAppPayloadException($"WeakApp value exceeds the maximum supported length of {maxLength}");
        }

        return normalized;
    }
}
