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
                    List<AggregationQueryRow> rows = await db.Apply(
                            new ReadingAggregationSpecification(
                                parameters.ToReadingFilter(),
                                ReadingAggregationDimension.Location
                            )
                        )
                        .ToListAsync(cancellationToken);

                    return Results.Ok(rows.Select(row => row.ToAggregationResponse()).ToArray());
                }
            )
            .ValidateWithFluentValidation<ReadingQueryParameters>(ReadingQueryParametersValidator.FilterRuleSet)
            .WithName("GetAggregationsByLocation")
            .WithSummary("Aggregate readings by source and location name.")
            .Produces<IReadOnlyList<AggregationResponse>>()
            .ProducesValidationProblem();

        api.MapGet(
                "/aggregations/by-type",
                async (
                    [AsParameters] ReadingQueryParameters parameters,
                    ApplicationDbContext db,
                    CancellationToken cancellationToken
                ) =>
                {
                    List<AggregationQueryRow> rows = await db.Apply(
                            new ReadingAggregationSpecification(
                                parameters.ToReadingFilter(),
                                ReadingAggregationDimension.Type
                            )
                        )
                        .ToListAsync(cancellationToken);

                    return Results.Ok(rows.Select(row => row.ToAggregationResponse()).ToArray());
                }
            )
            .ValidateWithFluentValidation<ReadingQueryParameters>(ReadingQueryParametersValidator.FilterRuleSet)
            .WithName("GetAggregationsByType")
            .WithSummary("Aggregate readings by source and meter type.")
            .Produces<IReadOnlyList<AggregationResponse>>()
            .ProducesValidationProblem();

        api.MapGet(
                "/aggregations/by-time",
                async (
                    [AsParameters] ReadingQueryParameters parameters,
                    ApplicationDbContext db,
                    CancellationToken cancellationToken
                ) =>
                {
                    List<AggregationQueryRow> rows = await db.Apply(
                            new ReadingAggregationSpecification(
                                parameters.ToReadingFilter(),
                                ReadingAggregationDimension.Time,
                                parameters.ToTimeBucket()
                            )
                        )
                        .ToListAsync(cancellationToken);

                    return Results.Ok(rows.Select(row => row.ToAggregationResponse()).ToArray());
                }
            )
            .ValidateWithFluentValidation<ReadingQueryParameters>(
                ReadingQueryParametersValidator.FilterRuleSet,
                ReadingQueryParametersValidator.TimeBucketRuleSet
            )
            .WithName("GetAggregationsByTime")
            .WithSummary("Aggregate readings into minute, hour, or day buckets.")
            .Produces<IReadOnlyList<AggregationResponse>>()
            .ProducesValidationProblem();

        return api;
    }
}
