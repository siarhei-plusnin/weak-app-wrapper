using WeakAppWrapper.WebApi.Api;
using WeakAppWrapper.WebApi.Models;

namespace WeakAppWrapper.WebApi.Mappings;

public static class ReadingQueryParameterMappings
{
    extension(ReadingQueryParameters parameters)
    {
        public ReadingFilter ToReadingFilter()
        {
            MetricKey? metricKey = MetricCatalog.TryParse(parameters.MetricKey, out MetricKey parsedMetricKey)
                ? parsedMetricKey
                : null;

            return new(
                parameters.Source,
                parameters.Name,
                parameters.Type,
                metricKey,
                parameters.From,
                parameters.To,
                parameters.MinValue,
                parameters.MaxValue
            );
        }

        public ReadingQueryRequest ToReadingQueryRequest()
        {
            _ = ReadingQueryParser.TryParseSortField(parameters.SortBy, out ReadingSortField sortBy);
            _ = ReadingQueryParser.TryParseSortDirection(
                parameters.SortDirection,
                out ReadingSortDirection sortDirection
            );

            return new(
                parameters.ToReadingFilter(),
                parameters.Page ?? 1,
                parameters.PageSize ?? ReadingQueryRequest.DefaultPageSize,
                sortBy,
                sortDirection
            );
        }

        public TimeBucket ToTimeBucket()
        {
            _ = ReadingQueryParser.TryParseTimeBucket(parameters.Bucket, out TimeBucket bucket);
            return bucket;
        }
    }
}
