using Microsoft.EntityFrameworkCore;
using WeakAppWrapper.Persistence.Infrastructure.Persistence;
using WeakAppWrapper.WebApi.Mappings;
using WeakAppWrapper.WebApi.Models;
using WeakAppWrapper.WebApi.Projections;
using WeakAppWrapper.WebApi.Specifications;

namespace WeakAppWrapper.WebApi.Api.Endpoints;

public static class ReadingEndpoints
{
    public static RouteGroupBuilder MapReadingEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet(
                "/readings",
                async (
                    [AsParameters] ReadingQueryParameters parameters,
                    ApplicationDbContext db,
                    CancellationToken cancellationToken
                ) =>
                {
                    ReadingQueryRequest request = parameters.ToReadingQueryRequest();
                    ReadingPageSpecification page = new(
                        request.Filter,
                        request.SortBy,
                        request.SortDirection,
                        request.Page,
                        request.PageSize
                    );

                    long totalCount = await db.Apply(page, criteriaOnly: true).LongCountAsync(cancellationToken);
                    List<ReadingProjection> rows = await db.Apply(page).ToListAsync(cancellationToken);
                    ReadingResponse[] items = rows.Select(row => row.ToReadingResponse()).ToArray();

                    return Results.Ok(new ReadingPageResponse(items, totalCount, request.Page, request.PageSize));
                }
            )
            .ValidateWithFluentValidation<ReadingQueryParameters>(
                ReadingQueryParametersValidator.FilterRuleSet,
                ReadingQueryParametersValidator.ReadingsRuleSet
            )
            .WithName("GetReadings")
            .WithSummary("Get a filtered, sorted page of readings.")
            .Produces<ReadingPageResponse>()
            .ProducesValidationProblem();

        api.MapGet(
                "/readings/latest",
                async (
                    [AsParameters] ReadingQueryParameters parameters,
                    ApplicationDbContext db,
                    CancellationToken cancellationToken
                ) =>
                {
                    List<ReadingProjection> rows = await db.Apply(
                            new LatestReadingsSpecification(parameters.ToReadingFilter())
                        )
                        .ToListAsync(cancellationToken);

                    return Results.Ok(rows.Select(row => row.ToReadingResponse()).ToArray());
                }
            )
            .ValidateWithFluentValidation<ReadingQueryParameters>(ReadingQueryParametersValidator.FilterRuleSet)
            .WithName("GetLatestReadings")
            .WithSummary("Get the latest reading for each source, location name, and type.")
            .Produces<IReadOnlyList<ReadingResponse>>()
            .ProducesValidationProblem();

        return api;
    }
}
