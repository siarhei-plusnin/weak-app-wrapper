using Microsoft.EntityFrameworkCore;
using WeakAppWrapper.Persistence.Infrastructure.Persistence;
using WeakAppWrapper.WebApi.Mappings;
using WeakAppWrapper.WebApi.Models;
using WeakAppWrapper.WebApi.Projections;
using WeakAppWrapper.WebApi.Specifications;

namespace WeakAppWrapper.WebApi.Api.Endpoints;

public static class AggregationEndpoints
{
    public static RouteGroupBuilder MapAggregationEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet(
                "/aggregations/by-location",
                async (
                    [AsParameters] ReadingQueryParameters parameters,
                    ApplicationDbContext db,
                    CancellationToken cancellationToken
                ) =>
                {
                    ReadingFilter filter = parameters.ToReadingFilter();
                    ReadingAggregationSpecification specification = new(
                        filter,
                        ReadingAggregationDimension.Location,
                        TimeBucket.Hour
                    );
                    List<AggregationQueryRow> rows = await db.Apply(specification).ToListAsync(cancellationToken);
                    return Results.Ok(rows.Select(row => row.ToAggregationResponse()).ToArray());
                }
            )
            .WithName("GetAggregationsByLocation")
            .WithSummary("Aggregate readings by source and location name.")
            .Produces<IReadOnlyList<AggregationResponse>>();

        api.MapGet(
                "/aggregations/by-type",
                async (
                    [AsParameters] ReadingQueryParameters parameters,
                    ApplicationDbContext db,
                    CancellationToken cancellationToken
                ) =>
                {
                    ReadingFilter filter = parameters.ToReadingFilter();
                    ReadingAggregationSpecification specification = new(
                        filter,
                        ReadingAggregationDimension.Type,
                        TimeBucket.Hour
                    );
                    List<AggregationQueryRow> rows = await db.Apply(specification).ToListAsync(cancellationToken);
                    return Results.Ok(rows.Select(row => row.ToAggregationResponse()).ToArray());
                }
            )
            .WithName("GetAggregationsByType")
            .WithSummary("Aggregate readings by source and meter type.")
            .Produces<IReadOnlyList<AggregationResponse>>();

        api.MapGet(
                "/aggregations/by-time",
                async (
                    [AsParameters] ReadingQueryParameters parameters,
                    ApplicationDbContext db,
                    CancellationToken cancellationToken
                ) =>
                {
                    ReadingFilter filter = parameters.ToReadingFilter();
                    ReadingAggregationSpecification specification = new(
                        filter,
                        ReadingAggregationDimension.Time,
                        parameters.ToTimeBucket()
                    );
                    List<AggregationQueryRow> rows = await db.Apply(specification).ToListAsync(cancellationToken);
                    return Results.Ok(rows.Select(row => row.ToAggregationResponse()).ToArray());
                }
            )
            .WithName("GetAggregationsByTime")
            .WithSummary("Aggregate readings into minute, hour, or day buckets.")
            .Produces<IReadOnlyList<AggregationResponse>>();

        return api;
    }
}
