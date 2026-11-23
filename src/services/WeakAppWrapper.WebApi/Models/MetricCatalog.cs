namespace WeakAppWrapper.WebApi.Models;

public sealed record MetricDefinition(
    string JsonKey,
    string ReadingType,
    MetricValueKind ValueKind,
    string JsonType,
    string ApiName
);

public static class MetricCatalog
{
    public static MetricDefinition GetDefinition(MetricKey metricKey) =>
        metricKey switch
        {
            MetricKey.Co2 => new("co2", "air_quality", MetricValueKind.Numeric, "number", "co2"),
            MetricKey.Pm25 => new("pm25", "air_quality", MetricValueKind.Numeric, "number", "pm25"),
            MetricKey.Humidity => new("humidity", "air_quality", MetricValueKind.Numeric, "number", "humidity"),
            MetricKey.Energy => new("energy", "energy", MetricValueKind.Numeric, "number", "energy"),
            MetricKey.MotionDetected => new(
                "motionDetected",
                "motion",
                MetricValueKind.Boolean,
                "boolean",
                "motionDetected"
            ),
            _ => throw new ArgumentOutOfRangeException(nameof(metricKey), metricKey, "Unknown metric key."),
        };

    public static bool TryParse(string? value, out MetricKey metricKey)
    {
        switch (value?.Trim().ToLowerInvariant())
        {
            case "co2":
                metricKey = MetricKey.Co2;
                return true;
            case "pm25":
                metricKey = MetricKey.Pm25;
                return true;
            case "humidity":
                metricKey = MetricKey.Humidity;
                return true;
            case "energy":
                metricKey = MetricKey.Energy;
                return true;
            case "motiondetected":
            case "motion_detected":
                metricKey = MetricKey.MotionDetected;
                return true;
            default:
                metricKey = default;
                return false;
        }
    }
}
