using WeakAppWrapper.WebApi.Models;

namespace WeakAppWrapper.WebApi.Api;

public static class ReadingQueryParser
{
    public static bool TryParseSortField(string? value, out ReadingSortField sortField)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            sortField = ReadingSortField.CapturedAt;
            return true;
        }

        string normalizedValue = value.Trim().ToLowerInvariant();
        sortField = normalizedValue switch
        {
            "capturedat" or "captured_at" => ReadingSortField.CapturedAt,
            "name" => ReadingSortField.Name,
            "type" => ReadingSortField.Type,
            "source" => ReadingSortField.Source,
            _ => default,
        };

        return normalizedValue is "capturedat" or "captured_at" or "name" or "type" or "source";
    }

    public static bool TryParseSortDirection(string? value, out ReadingSortDirection sortDirection)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            sortDirection = ReadingSortDirection.Descending;
            return true;
        }

        switch (value.Trim().ToLowerInvariant())
        {
            case "asc":
                sortDirection = ReadingSortDirection.Ascending;
                return true;
            case "desc":
                sortDirection = ReadingSortDirection.Descending;
                return true;
            default:
                sortDirection = ReadingSortDirection.Descending;
                return false;
        }
    }

    public static bool TryParseTimeBucket(string? value, out TimeBucket bucket)
    {
        switch (value?.Trim().ToLowerInvariant())
        {
            case null:
            case "":
            case "hour":
                bucket = TimeBucket.Hour;
                return true;
            case "minute":
                bucket = TimeBucket.Minute;
                return true;
            case "day":
                bucket = TimeBucket.Day;
                return true;
            default:
                bucket = default;
                return false;
        }
    }
}
