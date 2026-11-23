using FluentValidation;
using Microsoft.EntityFrameworkCore;
using WeakAppWrapper.Persistence.Infrastructure.Persistence;
using WeakAppWrapper.WebApi.GraphQL.Mappers;
using WeakAppWrapper.WebApi.GraphQL.Models;
using WeakAppWrapper.WebApi.Mappings;
using WeakAppWrapper.WebApi.Models;
using WeakAppWrapper.WebApi.Projections;
using WeakAppWrapper.WebApi.Specifications;

namespace WeakAppWrapper.WebApi.GraphQL;

[ExtendObjectType(typeof(ReadingQuery))]
public sealed class AggregationQuery(
    IDbContextFactory<ApplicationDbContext> contextFactory,
    IValidator<ReadingFilter> filterValidator
)
{
    public async Task<IReadOnlyList<AggregationResponse>> GetAggregationsByLocationAsync(
        ReadingFilterInput? filter,
        CancellationToken cancellationToken = default
    )
    {
        ReadingFilter readingFilter = filter?.ToReadingFilter() ?? ReadingFilter.Empty;

        await GraphQLValidation.ValidateAsync(filterValidator, readingFilter, cancellationToken);

        await using ApplicationDbContext db = await contextFactory.CreateDbContextAsync(cancellationToken);
        List<AggregationQueryRow> rows = await db.Apply(
                new ReadingAggregationSpecification(
                    readingFilter,
                    ReadingAggregationDimension.Location,
                    TimeBucket.Hour
                )
            )
            .ToListAsync(cancellationToken);

        return rows.Select(row => row.ToAggregationResponse()).ToArray();
    }

    public async Task<IReadOnlyList<AggregationResponse>> GetAggregationsByTypeAsync(
        ReadingFilterInput? filter,
        CancellationToken cancellationToken = default
    )
    {
        ReadingFilter readingFilter = filter?.ToReadingFilter() ?? ReadingFilter.Empty;

        await GraphQLValidation.ValidateAsync(filterValidator, readingFilter, cancellationToken);

        await using ApplicationDbContext db = await contextFactory.CreateDbContextAsync(cancellationToken);
        List<AggregationQueryRow> rows = await db.Apply(
                new ReadingAggregationSpecification(readingFilter, ReadingAggregationDimension.Type, TimeBucket.Hour)
            )
            .ToListAsync(cancellationToken);

        return rows.Select(row => row.ToAggregationResponse()).ToArray();
    }

    public async Task<IReadOnlyList<AggregationResponse>> GetAggregationsByTimeAsync(
        ReadingFilterInput? filter,
        TimeBucket bucket = TimeBucket.Hour,
        CancellationToken cancellationToken = default
    )
    {
        ReadingFilter readingFilter = filter?.ToReadingFilter() ?? ReadingFilter.Empty;

        await GraphQLValidation.ValidateAsync(filterValidator, readingFilter, cancellationToken);

        await using ApplicationDbContext db = await contextFactory.CreateDbContextAsync(cancellationToken);
        List<AggregationQueryRow> rows = await db.Apply(
                new ReadingAggregationSpecification(readingFilter, ReadingAggregationDimension.Time, bucket)
            )
            .ToListAsync(cancellationToken);

        return rows.Select(row => row.ToAggregationResponse()).ToArray();
    }
}
