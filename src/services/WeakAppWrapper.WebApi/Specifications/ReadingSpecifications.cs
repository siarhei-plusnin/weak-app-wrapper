using System.Linq.Expressions;
using WeakAppWrapper.Persistence.Domain;
using WeakAppWrapper.WebApi.Models;

namespace WeakAppWrapper.WebApi.Specifications;

public sealed class ReadingPageSpecification : Specification<Reading>
{
    public ReadingPageSpecification(
        ReadingFilter filter,
        ReadingSortField sortBy,
        ReadingSortDirection sortDirection,
        int page,
        int pageSize
    )
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(page);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageSize);

        Filter = filter;
        Criteria = ReadingFilterCriteria.Create(filter);
        ApplySorting(sortBy, sortDirection);
        ApplyPaging(checked((page - 1) * pageSize), pageSize);
    }

    public ReadingFilter Filter { get; }

    private void ApplySorting(ReadingSortField field, ReadingSortDirection direction)
    {
        switch (field, direction)
        {
            case (ReadingSortField.CapturedAt, ReadingSortDirection.Ascending):
                ApplyOrderBy(reading => reading.CapturedAt);
                ApplyThenBy(reading => reading.Id);
                break;
            case (ReadingSortField.CapturedAt, ReadingSortDirection.Descending):
                ApplyOrderByDescending(reading => reading.CapturedAt);
                ApplyThenByDescending(reading => reading.Id);
                break;
            case (ReadingSortField.Name, ReadingSortDirection.Ascending):
                ApplyOrderBy(reading => reading.Name);
                ApplyThenByDescending(reading => reading.CapturedAt);
                ApplyThenByDescending(reading => reading.Id);
                break;
            case (ReadingSortField.Name, ReadingSortDirection.Descending):
                ApplyOrderByDescending(reading => reading.Name);
                ApplyThenByDescending(reading => reading.CapturedAt);
                ApplyThenByDescending(reading => reading.Id);
                break;
            case (ReadingSortField.Type, ReadingSortDirection.Ascending):
                ApplyOrderBy(reading => reading.Type);
                ApplyThenByDescending(reading => reading.CapturedAt);
                ApplyThenByDescending(reading => reading.Id);
                break;
            case (ReadingSortField.Type, ReadingSortDirection.Descending):
                ApplyOrderByDescending(reading => reading.Type);
                ApplyThenByDescending(reading => reading.CapturedAt);
                ApplyThenByDescending(reading => reading.Id);
                break;
            case (ReadingSortField.Source, ReadingSortDirection.Ascending):
                ApplyOrderBy(reading => reading.Source);
                ApplyThenByDescending(reading => reading.CapturedAt);
                ApplyThenByDescending(reading => reading.Id);
                break;
            case (ReadingSortField.Source, ReadingSortDirection.Descending):
                ApplyOrderByDescending(reading => reading.Source);
                ApplyThenByDescending(reading => reading.CapturedAt);
                ApplyThenByDescending(reading => reading.Id);
                break;
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(field),
                    field,
                    "Unsupported reading sort specification."
                );
        }
    }
}

public sealed class LatestReadingsSpecification : Specification<Reading>
{
    public LatestReadingsSpecification(ReadingFilter filter)
    {
        ArgumentNullException.ThrowIfNull(filter);

        Filter = filter;
        Criteria = ReadingFilterCriteria.Create(filter);
        ApplyOrderByDescending(reading => reading.CapturedAt);
        ApplyThenByDescending(reading => reading.Id);
    }

    public ReadingFilter Filter { get; }
}

public sealed record ReadingAggregationSpecification(
    ReadingFilter Filter,
    ReadingAggregationDimension Dimension,
    TimeBucket Bucket = TimeBucket.Hour
);

public enum ReadingAggregationDimension
{
    Location,
    Type,
    Time,
}

internal static class ReadingFilterCriteria
{
    public static Expression<Func<Reading, bool>> Create(ReadingFilter filter) =>
        reading =>
            (filter.Source == null || reading.Source == filter.Source)
            && (filter.Name == null || reading.Name == filter.Name)
            && (filter.Type == null || reading.Type == filter.Type)
            && (filter.From == null || reading.CapturedAt >= filter.From.Value)
            && (filter.To == null || reading.CapturedAt <= filter.To.Value);
}
