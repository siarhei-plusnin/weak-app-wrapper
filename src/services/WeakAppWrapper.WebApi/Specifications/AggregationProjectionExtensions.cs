using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using WeakAppWrapper.Persistence.Infrastructure.Persistence;
using WeakAppWrapper.WebApi.Models;
using WeakAppWrapper.WebApi.Projections;

namespace WeakAppWrapper.WebApi.Specifications;

public static class AggregationProjectionExtensions
{
    extension(ApplicationDbContext db)
    {
        public IQueryable<AggregationQueryRow> Apply(ReadingAggregationSpecification specification)
        {
            ReadingFilter filter = specification.Filter;
            MetricDefinition? metric = filter.MetricKey is MetricKey metricKey
                ? MetricCatalog.GetDefinition(metricKey)
                : null;
            (string selectedDimension, string groupBy) = GetDimensionSql(specification);
            string sql = $"""
                SELECT
                    source AS "Source",
                    {selectedDimension},
                    @metricApiName AS "MetricKey",
                    COUNT(*)::bigint AS "Count",
                    SUM(CASE
                        WHEN @metricJsonType = 'number'
                            AND jsonb_typeof(raw_record -> 'payload' -> @metricKey) = 'number'
                        THEN (raw_record -> 'payload' ->> @metricKey)::double precision
                        ELSE NULL::double precision
                    END) AS "Sum",
                    AVG(CASE
                        WHEN @metricJsonType = 'number'
                            AND jsonb_typeof(raw_record -> 'payload' -> @metricKey) = 'number'
                        THEN (raw_record -> 'payload' ->> @metricKey)::double precision
                        ELSE NULL::double precision
                    END) AS "Average",
                    MIN(CASE
                        WHEN @metricJsonType = 'number'
                            AND jsonb_typeof(raw_record -> 'payload' -> @metricKey) = 'number'
                        THEN (raw_record -> 'payload' ->> @metricKey)::double precision
                        ELSE NULL::double precision
                    END) AS "Minimum",
                    MAX(CASE
                        WHEN @metricJsonType = 'number'
                            AND jsonb_typeof(raw_record -> 'payload' -> @metricKey) = 'number'
                        THEN (raw_record -> 'payload' ->> @metricKey)::double precision
                        ELSE NULL::double precision
                    END) AS "Maximum",
                    CASE WHEN @metricJsonType = 'boolean'
                        THEN COUNT(*) FILTER (WHERE raw_record -> 'payload' ->> @metricKey = 'true')::bigint
                        ELSE NULL::bigint
                    END AS "TrueCount",
                    CASE WHEN @metricJsonType = 'boolean'
                        THEN COUNT(*) FILTER (WHERE raw_record -> 'payload' ->> @metricKey = 'false')::bigint
                        ELSE NULL::bigint
                    END AS "FalseCount",
                    CASE WHEN @metricJsonType = 'boolean'
                        THEN COUNT(*) FILTER (WHERE raw_record -> 'payload' ->> @metricKey = 'true')::double precision
                            / NULLIF(COUNT(*), 0)
                        ELSE NULL::double precision
                    END AS "TrueRate"
                FROM readings
                WHERE (@source IS NULL OR source = @source)
                    AND (@name IS NULL OR name = @name)
                    AND (@type IS NULL OR type = @type)
                    AND (@from IS NULL OR captured_at >= @from)
                    AND (@to IS NULL OR captured_at <= @to)
                    AND (
                        @metricKey IS NULL
                        OR (
                            type = @metricType
                            AND jsonb_typeof(raw_record -> 'payload' -> @metricKey) = @metricJsonType
                        )
                    )
                    AND (
                        @minimum IS NULL
                        OR CASE
                            WHEN jsonb_typeof(raw_record -> 'payload' -> @metricKey) = 'number'
                            THEN (raw_record -> 'payload' ->> @metricKey)::double precision
                            ELSE NULL
                        END >= @minimum
                    )
                    AND (
                        @maximum IS NULL
                        OR CASE
                            WHEN jsonb_typeof(raw_record -> 'payload' -> @metricKey) = 'number'
                            THEN (raw_record -> 'payload' ->> @metricKey)::double precision
                            ELSE NULL
                        END <= @maximum
                    )
                GROUP BY {groupBy}
                ORDER BY {groupBy}
                """;

            return db.Database.SqlQueryRaw<AggregationQueryRow>(
                sql,
                [
                    CreateParameter("source", NpgsqlDbType.Text, filter.Source),
                    CreateParameter("name", NpgsqlDbType.Text, filter.Name),
                    CreateParameter("type", NpgsqlDbType.Text, filter.Type),
                    CreateParameter("from", NpgsqlDbType.TimestampTz, filter.From),
                    CreateParameter("to", NpgsqlDbType.TimestampTz, filter.To),
                    CreateParameter("metricKey", NpgsqlDbType.Text, metric?.JsonKey),
                    CreateParameter("metricType", NpgsqlDbType.Text, metric?.ReadingType),
                    CreateParameter("metricJsonType", NpgsqlDbType.Text, metric?.JsonType),
                    CreateParameter("metricApiName", NpgsqlDbType.Text, metric?.ApiName),
                    CreateParameter("minimum", NpgsqlDbType.Double, filter.MinValue),
                    CreateParameter("maximum", NpgsqlDbType.Double, filter.MaxValue),
                ]
            );
        }
    }

    private static (string Select, string GroupBy) GetDimensionSql(ReadingAggregationSpecification specification)
    {
        if (specification.Dimension == ReadingAggregationDimension.Time)
        {
            string bucket = specification.Bucket switch
            {
                TimeBucket.Minute => "minute",
                TimeBucket.Hour => "hour",
                TimeBucket.Day => "day",
                _ => throw new ArgumentOutOfRangeException(nameof(specification), "Unsupported time bucket."),
            };
            string expression = $"date_trunc('{bucket}', captured_at)";
            return (
                $"NULL::text AS \"Name\", NULL::text AS \"Type\", {expression} AS \"BucketStart\"",
                $"source, {expression}"
            );
        }

        return specification.Dimension switch
        {
            ReadingAggregationDimension.Location => (
                "name AS \"Name\", NULL::text AS \"Type\", NULL::timestamptz AS \"BucketStart\"",
                "source, name"
            ),
            ReadingAggregationDimension.Type => (
                "NULL::text AS \"Name\", type AS \"Type\", NULL::timestamptz AS \"BucketStart\"",
                "source, type"
            ),
            _ => throw new ArgumentOutOfRangeException(nameof(specification), "Unsupported aggregation dimension."),
        };
    }

    private static NpgsqlParameter CreateParameter(string name, NpgsqlDbType type, object? value) =>
        new(name, type) { Value = value ?? DBNull.Value };
}
