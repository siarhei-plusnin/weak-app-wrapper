namespace WeakAppWrapper.WebApi.Specifications;

public static class SpecificationEvaluator
{
    public static IQueryable<TEntity> GetQuery<TEntity>(
        IQueryable<TEntity> query,
        Specification<TEntity> specification,
        bool criteriaOnly = false
    )
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(specification);

        if (specification.Criteria is not null)
        {
            query = query.Where(specification.Criteria);
        }

        if (criteriaOnly)
        {
            return query;
        }

        IOrderedQueryable<TEntity>? orderedQuery = null;
        foreach (IOrderClause<TEntity> ordering in specification.Orderings)
        {
            orderedQuery = orderedQuery is null
                ? ordering.ApplyOrderBy(query)
                : ordering.ApplyThenBy(orderedQuery);
        }

        query = orderedQuery ?? query;

        if (specification.IsPagingEnabled)
        {
            if (orderedQuery is null)
            {
                throw new InvalidOperationException("Paging requires an ordering.");
            }

            query = query.Skip(specification.Skip!.Value).Take(specification.Take!.Value);
        }

        return query;
    }
}
