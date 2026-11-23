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

public sealed class ReadingQuery(
    IDbContextFactory<ApplicationDbContext> contextFactory,
    IValidator<ReadingFilter> filterValidator,
    IValidator<ReadingQueryRequest> queryRequestValidator,
    IValidator<LatestMetricValuesRequest> latestMetricValuesRequestValidator
)
{
    public async Task<ReadingPageGraph> GetReadingsAsync(
        ReadingFilterInput? filter,
        int page = 1,
        int pageSize = ReadingQueryRequest.DefaultPageSize,
        ReadingSortField sortBy = ReadingSortField.CapturedAt,
        ReadingSortDirection sortDirection = ReadingSortDirection.Descending,
        CancellationToken cancellationToken = default
    )
    {
        ReadingQueryRequest request = new(
            filter?.ToReadingFilter() ?? ReadingFilter.Empty,
            page,
            pageSize,
            sortBy,
            sortDirection
        );
        await GraphQLValidation.ValidateAsync(queryRequestValidator, request, cancellationToken);

        ReadingPageSpecification pageSpecification = new(
            request.Filter,
            request.SortBy,
            request.SortDirection,
            request.Page,
            request.PageSize
        );
        await using ApplicationDbContext db = await contextFactory.CreateDbContextAsync(cancellationToken);

        long totalCount = await db.Apply(pageSpecification, criteriaOnly: true).LongCountAsync(cancellationToken);
        List<ReadingProjection> rows = await db.Apply(pageSpecification).ToListAsync(cancellationToken);
        ReadingResponse[] items = rows.Select(row => row.ToReadingResponse()).ToArray();

        return new ReadingPageResponse(items, totalCount, request.Page, request.PageSize).ToGraphPage();
    }

    public async Task<IReadOnlyList<GraphReading>> GetLatestReadingsAsync(
        ReadingFilterInput? filter,
        CancellationToken cancellationToken = default
    )
    {
        ReadingFilter readingFilter = filter?.ToReadingFilter() ?? ReadingFilter.Empty;
        await GraphQLValidation.ValidateAsync(filterValidator, readingFilter, cancellationToken);
        await using ApplicationDbContext db = await contextFactory.CreateDbContextAsync(cancellationToken);

        List<ReadingProjection> rows = await db.Apply(new LatestReadingsSpecification(readingFilter))
            .ToListAsync(cancellationToken);
        return rows.Select(row => row.ToReadingResponse().ToGraphReading()).ToArray();
    }

    public async Task<IReadOnlyList<LatestMetricValueResponse>> GetLatestValuesAsync(
        MetricKey metricKey,
        ReadingFilterInput? filter,
        CancellationToken cancellationToken = default
    )
    {
        LatestMetricValuesRequest request = new(metricKey, filter?.ToReadingFilter() ?? ReadingFilter.Empty);
        await GraphQLValidation.ValidateAsync(latestMetricValuesRequestValidator, request, cancellationToken);
        ReadingFilter readingFilter = request.Filter with { MetricKey = metricKey };
        await using ApplicationDbContext db = await contextFactory.CreateDbContextAsync(cancellationToken);

        List<ReadingProjection> rows = await db.Apply(new LatestReadingsSpecification(readingFilter))
            .ToListAsync(cancellationToken);
        MetricDefinition definition = MetricCatalog.GetDefinition(metricKey);
        return rows.Select(row => row.ToReadingResponse().ToLatestMetricValue(definition)).ToArray();
    }
}
