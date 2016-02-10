using System;
using System.Linq;
using System.Linq.Expressions;
using Mapster.Utils;

namespace Mapster
{
    public static class QueryableExtensions
    {
        [Obsolete("Please use 'ProjectToType' method instead")]
        public static IProjectionExpression Project<TSource>(this IQueryable<TSource> source)
        {
            return new ProjectionExpression<TSource>(source);
        }

        public static IQueryable<TDestination> ProjectToType<TDestination>(this IQueryable source, TypeAdapterConfig config = null)
        {
            config = config ?? TypeAdapterConfig.GlobalSettings;
            var mockCall = config.GetProjectionCallExpression(source.ElementType, typeof(TDestination));
            var sourceCall = Expression.Call(mockCall.Method, source.Expression, mockCall.Arguments[1]);
            return source.Provider.CreateQuery<TDestination>(sourceCall);
        } 
    }
}
