using System.Linq.Expressions;

namespace WeakAppWrapper.WebApi.Specifications;

public abstract class Specification<TEntity>
    where TEntity : class
{
    private readonly List<IOrderClause<TEntity>> _orderings = [];

    public Expression<Func<TEntity, bool>>? Criteria { get; protected set; }

    public int? Skip { get; private set; }

    public int? Take { get; private set; }

    public bool IsPagingEnabled { get; private set; }

    internal IReadOnlyList<IOrderClause<TEntity>> Orderings => _orderings;

    protected void ApplyOrderBy<TKey>(Expression<Func<TEntity, TKey>> keySelector) =>
        AddOrdering(keySelector, descending: false, isPrimary: true);

    protected void ApplyOrderByDescending<TKey>(Expression<Func<TEntity, TKey>> keySelector) =>
        AddOrdering(keySelector, descending: true, isPrimary: true);

    protected void ApplyThenBy<TKey>(Expression<Func<TEntity, TKey>> keySelector) =>
        AddOrdering(keySelector, descending: false, isPrimary: false);

    protected void ApplyThenByDescending<TKey>(Expression<Func<TEntity, TKey>> keySelector) =>
        AddOrdering(keySelector, descending: true, isPrimary: false);

    protected void ApplyPaging(int skip, int take)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(skip);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(take);

        Skip = skip;
        Take = take;
        IsPagingEnabled = true;
    }

    private void AddOrdering<TKey>(
        Expression<Func<TEntity, TKey>> keySelector,
        bool descending,
        bool isPrimary
    )
    {
        ArgumentNullException.ThrowIfNull(keySelector);

        if (isPrimary == (_orderings.Count > 0))
        {
            throw new InvalidOperationException(
                isPrimary ? "Only one primary ordering can be applied." : "A secondary ordering requires a primary ordering."
            );
        }

        _orderings.Add(new OrderClause<TEntity, TKey>(keySelector, descending));
    }
}

internal interface IOrderClause<TEntity>
    where TEntity : class
{
    IOrderedQueryable<TEntity> ApplyOrderBy(IQueryable<TEntity> query);

    IOrderedQueryable<TEntity> ApplyThenBy(IOrderedQueryable<TEntity> query);
}

internal sealed class OrderClause<TEntity, TKey>(
    Expression<Func<TEntity, TKey>> keySelector,
    bool descending
) : IOrderClause<TEntity>
    where TEntity : class
{
    public IOrderedQueryable<TEntity> ApplyOrderBy(IQueryable<TEntity> query) =>
        descending ? query.OrderByDescending(keySelector) : query.OrderBy(keySelector);

    public IOrderedQueryable<TEntity> ApplyThenBy(IOrderedQueryable<TEntity> query) =>
        descending ? query.ThenByDescending(keySelector) : query.ThenBy(keySelector);
}
