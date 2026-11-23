using System.Text.Json;
using WeakAppWrapper.WebApi.GraphQL.Models;
using WeakAppWrapper.WebApi.Models;

namespace WeakAppWrapper.WebApi.GraphQL.Mappers;

public static class ReadingGraphReadingMappingExtensions
{
    extension(ReadingResponse reading)
    {
        public GraphReading ToGraphReading()
        {
            ReadingPayload payload = reading.Type switch
            {
                "air_quality" => new AirQualityPayload(
                    GetNumber(reading.Payload, "co2"),
                    GetNumber(reading.Payload, "pm25"),
                    GetNumber(reading.Payload, "humidity")
                ),
                "energy" => new EnergyPayload(GetNumber(reading.Payload, "energy")),
                "motion" => new MotionPayload(GetBoolean(reading.Payload, "motionDetected")),
                _ => new UnknownPayload(reading.Type, reading.Payload.GetRawText()),
            };

            return new(
                reading.Id,
                reading.Source,
                reading.Type,
                reading.Name,
                reading.LocationName,
                reading.CapturedAt,
                reading.CreatedAt,
                payload
            );
        }
    }

    private static double? GetNumber(JsonElement payload, string propertyName) =>
        payload.ValueKind == JsonValueKind.Object
        && payload.TryGetProperty(propertyName, out JsonElement value)
        && value.ValueKind == JsonValueKind.Number
        && value.TryGetDouble(out double number)
            ? number
            : null;

    private static bool? GetBoolean(JsonElement payload, string propertyName) =>
        payload.ValueKind == JsonValueKind.Object
        && payload.TryGetProperty(propertyName, out JsonElement value)
        && value.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? value.GetBoolean()
            : null;
}

public static class ReadingGraphPageMappingExtensions
{
    extension(ReadingPageResponse page)
    {
        public ReadingPageGraph ToGraphPage() =>
            new(
                page.Items.Select(reading => reading.ToGraphReading()).ToArray(),
                page.TotalCount,
                page.Page,
                page.PageSize
            );
    }
}
