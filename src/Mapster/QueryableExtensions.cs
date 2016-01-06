using System.Linq;
using Mapster.Utils;

namespace Mapster
{
    public static class QueryableExtensions
    {
        public static IProjectionExpression Project<TSource>(this IQueryable<TSource> source)
        {
            return new ProjectionExpression<TSource>(source);
        }
    }
}
