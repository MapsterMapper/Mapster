using System;
using System.Linq.Expressions;

namespace Mapster.Adapters
{
    public abstract class BaseInlineAdapter : ITypeAdapterWithTarget, IInlineTypeAdapter
    {
        public abstract bool CanAdapt(Type sourceType, Type desinationType);

        public Func<TSource, TDestination> CreateAdaptFunc<TSource, TDestination>()
        {
            //var depth = Expression.Parameter(typeof (int));
            var p = Expression.Parameter(typeof(TSource));
            var body = CreateExpression(p, null, typeof(TDestination));
            return Expression.Lambda<Func<TSource, TDestination>>(body, p).Compile();
        }

        public Func<TSource, TDestination, TDestination> CreateAdaptTargetFunc<TSource, TDestination>()
        {
            //var depth = Expression.Parameter(typeof(int));
            var p = Expression.Parameter(typeof(TSource));
            var p2 = Expression.Parameter(typeof(TDestination));
            var body = CreateExpression(p, p2, typeof(TDestination));
            return Expression.Lambda<Func<TSource, TDestination, TDestination>>(body, p, p2).Compile();
        }

        public abstract Expression CreateExpression(Expression source, Expression destination, Type destinationType);
    }
}
