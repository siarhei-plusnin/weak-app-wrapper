using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using WeakAppWrapper.Persistence.Domain;
using WeakAppWrapper.Persistence.Infrastructure.Persistence;
using WeakAppWrapper.WebApi.Models;
using WeakAppWrapper.WebApi.Projections;

namespace WeakAppWrapper.WebApi.Specifications;

public static class ReadingProjectionExtensions
{
    private static readonly Expression<Func<Reading, ReadingProjection>> s_projection = reading => new(
        reading.Id,
        reading.Source,
        reading.Type,
        reading.Name,
        reading.RawRecord,
        reading.CapturedAt,
        reading.CreatedAt
    );

    extension(ApplicationDbContext db)
    {
        public IQueryable<ReadingProjection> Apply(
            ReadingPageSpecification specification,
            bool criteriaOnly = false
        ) =>
            SpecificationEvaluator
                .GetQuery(GetFilteredReadings(db, specification.Filter), specification, criteriaOnly)
                .Select(s_projection);

        public IQueryable<ReadingProjection> Apply(LatestReadingsSpecification specification) =>
            SpecificationEvaluator
                .GetQuery(ApplyLatest(db, specification.Filter), specification)
                .Select(s_projection);
    }

    private static IQueryable<Reading> GetFilteredReadings(ApplicationDbContext db, ReadingFilter filter)
    {
        MetricDefinition? metric = GetMetric(filter);
        IQueryable<Reading> query = db.Readings.AsNoTracking();

        if (metric is not null)
        {
            // Metric fields live in JSONB, so their type/range predicates are PostgreSQL-specific.
            query = db.Readings
                .FromSqlInterpolated($"""
                    SELECT *
                    FROM readings
                    WHERE type = {metric.ReadingType}
                        AND jsonb_typeof(raw_record -> 'payload' -> {metric.JsonKey}) = {metric.JsonType}
                        AND ({filter.MinValue}::double precision IS NULL OR CASE
                            WHEN jsonb_typeof(raw_record -> 'payload' -> {metric.JsonKey}) = 'number'
                            THEN (raw_record -> 'payload' ->> {metric.JsonKey})::double precision
                            ELSE NULL
                        END >= {filter.MinValue})
                        AND ({filter.MaxValue}::double precision IS NULL OR CASE
                            WHEN jsonb_typeof(raw_record -> 'payload' -> {metric.JsonKey}) = 'number'
                            THEN (raw_record -> 'payload' ->> {metric.JsonKey})::double precision
                            ELSE NULL
                        END <= {filter.MaxValue})
                    """)
                .AsNoTracking();
        }

        return query;
    }

    private static IQueryable<Reading> ApplyLatest(ApplicationDbContext db, ReadingFilter filter)
    {
        MetricDefinition? metric = GetMetric(filter);
        string? metricKey = metric?.JsonKey;
        string? metricType = metric?.ReadingType;
        string? metricJsonType = metric?.JsonType;

        return db.Readings
            .FromSqlInterpolated($"""
                SELECT DISTINCT ON (source, name, type) *
                FROM readings
                WHERE ({filter.Source}::text IS NULL OR source = {filter.Source})
                    AND ({filter.Name}::text IS NULL OR name = {filter.Name})
                    AND ({filter.Type}::text IS NULL OR type = {filter.Type})
                    AND ({filter.From}::timestamptz IS NULL OR captured_at >= {filter.From})
                    AND ({filter.To}::timestamptz IS NULL OR captured_at <= {filter.To})
                    AND (
                        {metricKey}::text IS NULL
                        OR (
                            type = {metricType}
                            AND jsonb_typeof(raw_record -> 'payload' -> {metricKey}) = {metricJsonType}
                        )
                    )
                    AND (
                        {filter.MinValue}::double precision IS NULL
                        OR CASE
                            WHEN jsonb_typeof(raw_record -> 'payload' -> {metricKey}) = 'number'
                            THEN (raw_record -> 'payload' ->> {metricKey})::double precision
                            ELSE NULL
                        END >= {filter.MinValue}
                    )
                    AND (
                        {filter.MaxValue}::double precision IS NULL
                        OR CASE
                            WHEN jsonb_typeof(raw_record -> 'payload' -> {metricKey}) = 'number'
                            THEN (raw_record -> 'payload' ->> {metricKey})::double precision
                            ELSE NULL
                        END <= {filter.MaxValue}
                    )
                ORDER BY source, name, type, captured_at DESC, id DESC
                """)
            .AsNoTracking();
    }

    private static MetricDefinition? GetMetric(ReadingFilter filter) =>
        filter.MetricKey is MetricKey metricKey ? MetricCatalog.GetDefinition(metricKey) : null;
}
