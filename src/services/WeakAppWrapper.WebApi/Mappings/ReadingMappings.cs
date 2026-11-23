using System.Text.Json;
using WeakAppWrapper.WebApi.Models;
using WeakAppWrapper.WebApi.Projections;

namespace WeakAppWrapper.WebApi.Mappings;

public static class ReadingProjectionMappingExtensions
{
    extension(ReadingProjection reading)
    {
        public ReadingResponse ToReadingResponse()
        {
            using JsonDocument document = JsonDocument.Parse(reading.RawRecord);
            JsonElement payload;

            if (
                document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty("payload", out JsonElement sourcePayload)
            )
            {
                payload = sourcePayload.Clone();
            }
            else
            {
                using JsonDocument emptyPayload = JsonDocument.Parse("{}");
                payload = emptyPayload.RootElement.Clone();
            }

            return new(
                reading.Id,
                reading.Source,
                reading.Type,
                reading.Name,
                reading.CapturedAt,
                reading.CreatedAt,
                payload
            );
        }
    }
}

public static class ReadingResponseMappingExtensions
{
    extension(ReadingResponse reading)
    {
        public LatestMetricValueResponse ToLatestMetricValue(MetricDefinition definition)
        {
            JsonElement value =
                reading.Payload.ValueKind == JsonValueKind.Object
                && reading.Payload.TryGetProperty(definition.JsonKey, out JsonElement payloadValue)
                    ? payloadValue
                    : default;

            double? numericValue =
                definition.ValueKind == MetricValueKind.Numeric
                && value.ValueKind == JsonValueKind.Number
                && value.TryGetDouble(out double numeric)
                    ? numeric
                    : null;
            bool? booleanValue =
                definition.ValueKind == MetricValueKind.Boolean
                && value.ValueKind is JsonValueKind.True or JsonValueKind.False
                    ? value.GetBoolean()
                    : null;

            return new(
                reading.Id,
                reading.Source,
                reading.Name,
                reading.Type,
                definition.ApiName,
                reading.CapturedAt,
                numericValue,
                booleanValue
            );
        }
    }
}

public static class AggregationRowMappingExtensions
{
    extension(AggregationQueryRow row)
    {
        internal AggregationResponse ToAggregationResponse() =>
            new(
                row.Source,
                row.Name,
                row.Type,
                row.BucketStart,
                row.MetricKey,
                row.Count,
                row.Sum,
                row.Average,
                row.Minimum,
                row.Maximum,
                row.TrueCount,
                row.FalseCount,
                row.TrueRate
            );
    }
}
