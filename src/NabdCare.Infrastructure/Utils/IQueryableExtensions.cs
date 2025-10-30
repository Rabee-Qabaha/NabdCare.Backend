using System.Linq.Expressions;

namespace NabdCare.Infrastructure.Utils;

public static class QueryableExtensions
{
    public static IQueryable<T> OrderByDynamic<T>(this IQueryable<T> source, string propertyName)
    {
        var param = Expression.Parameter(typeof(T));
        var property = Expression.Property(param, propertyName);
        var sort = Expression.Lambda(property, param);
        var method = typeof(Queryable).GetMethods()
            .First(m => m.Name == "OrderBy" && m.GetParameters().Length == 2)
            .MakeGenericMethod(typeof(T), property.Type);
        return (IQueryable<T>)method.Invoke(null, new object[] { source, sort })!;
    }

    public static IQueryable<T> OrderByDescendingDynamic<T>(this IQueryable<T> source, string propertyName)
    {
        var param = Expression.Parameter(typeof(T));
        var property = Expression.Property(param, propertyName);
        var sort = Expression.Lambda(property, param);
        var method = typeof(Queryable).GetMethods()
            .First(m => m.Name == "OrderByDescending" && m.GetParameters().Length == 2)
            .MakeGenericMethod(typeof(T), property.Type);
        return (IQueryable<T>)method.Invoke(null, [source, sort])!;
    }
}