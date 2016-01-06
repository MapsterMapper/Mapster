using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Mapster.Utils;

namespace Mapster.Adapters
{
    internal static class PrimitiveAdapter<TSource, TDestination>
    {
        public static Expression<Func<int, TSource, TDestination>> CreateAdaptFunc()
        {
            var depth = Expression.Parameter(typeof (int));
            var p = Expression.Parameter(typeof (TSource));
            var body = CreateExpressionBody(depth, p);
            return Expression.Lambda<Func<int, TSource, TDestination>>(body, depth, p);
        }

        public static Expression<Func<int, TSource, TDestination, TDestination>> CreateAdaptTargetFunc()
        {
            var depth = Expression.Parameter(typeof(int));
            var p = Expression.Parameter(typeof(TSource));
            var p2 = Expression.Parameter(typeof (TDestination));
            var body = CreateExpressionBody(depth, p);
            return Expression.Lambda<Func<int, TSource, TDestination, TDestination>>(body, depth, p, p2);
        }

        private static Expression CreateExpressionBody(ParameterExpression depth, ParameterExpression p)
        {
            var sourceType = typeof (TSource);
            var destinationType = typeof (TDestination);
            var list = new List<Expression>();

            var pDest = Expression.Variable(destinationType);
            Expression convert = p;
            if (sourceType != destinationType)
            {
                if (sourceType.IsNullable())
                {
                    convert = Expression.Convert(convert, sourceType.GetGenericArguments()[0]);
                }
                convert = ReflectionUtils.BuildConvertExpression<TSource, TDestination>(convert);
                if (convert.Type != destinationType)
                    convert = Expression.Convert(convert, destinationType);
            }
            if ((!sourceType.IsValueType || sourceType.IsNullable()) && destinationType.IsValueType && !destinationType.IsNullable())
            {
                var compareNull = Expression.Equal(p, Expression.Constant(null, sourceType));
                convert = Expression.Condition(compareNull, Expression.Constant(default(TDestination), typeof(TDestination)), convert);
            }

            var setting = TypeAdapterConfig<TSource, TDestination>.ConfigSettings;
            if (setting?.MaxDepth != null && setting.MaxDepth.Value > 0)
            {
                var compareDepth = Expression.GreaterThan(depth, Expression.Constant(setting.MaxDepth.Value));
                convert = Expression.Condition(compareDepth, Expression.Constant(default(TDestination), typeof(TDestination)), convert);
            }

            list.Add(Expression.Assign(pDest, convert));
            var destinationTransforms = setting != null
                ? setting.DestinationTransforms.Transforms
                : TypeAdapterConfig.GlobalSettings.DestinationTransforms.Transforms;
            if (destinationTransforms.ContainsKey(destinationType))
            {
                var transform = TypeAdapterConfig.GlobalSettings.DestinationTransforms.Transforms[destinationType];
                var invoke = Expression.Invoke(transform, pDest);
                list.Add(Expression.Assign(pDest, invoke));
            }
            list.Add(pDest);
            return Expression.Block(destinationType, new[] {pDest}, list);
        }
    }
}
