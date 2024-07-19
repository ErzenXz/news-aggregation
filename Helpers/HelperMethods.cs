using System.Linq.Expressions;

namespace NewsAggregation.Helpers;


public static class HelperMethods
{
    public static IQueryable<T> WhereIf<T>(this IQueryable<T> query, bool searchCondition, Expression<Func<T, bool>> predicate)
    {
        return searchCondition ? query.Where(predicate) : query;
    }
}