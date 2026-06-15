using System.Text.Json;
using WeakAppWrapper.Contracts.Messages;

namespace WeakAppWrapper.Processor.Application;

public static class WeakAppReadingNormalizer
{
    private const string DefaultSource = "weakapp";

    public static IReadOnlyList<NormalizedReading> Normalize(WeakAppMetersPolledMessage message)
    {
        if (message.Payload.ValueKind != JsonValueKind.Array)
        {
            throw new WeakAppPayloadException("WeakApp payload root must be a JSON array of meters");
        }

        return message
            .Payload.EnumerateArray()
            .Select(meter =>
            {
                if (meter.ValueKind != JsonValueKind.Object)
                {
                    throw new WeakAppPayloadException("WeakApp meter entries must be JSON objects");
                }

                return new NormalizedReading(
                    string.IsNullOrWhiteSpace(message.Source) ? DefaultSource : message.Source.Trim(),
                    ReadRequiredString(meter, "type", "WeakApp meter is missing a type"),
                    ReadRequiredString(meter, "name", "WeakApp meter is missing a name"),
                    ReadPayloadObject(meter).Clone(),
                    message.FetchedAt,
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

    private static string ReadRequiredString(JsonElement element, string propertyName, string errorMessage) =>
        TryReadString(element, propertyName)?.Trim() ?? throw new WeakAppPayloadException(errorMessage);

    private static string? TryReadString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out JsonElement property))
        {
            return null;
        }

        return property.ValueKind == JsonValueKind.String ? property.GetString() : property.GetRawText();
    }
}
