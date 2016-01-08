using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Mapster.Models;
using Mapster.Utils;

namespace Mapster.Adapters
{
    internal static class PrimitiveAdapter<TSource, TDestination>
    {
        public static Expression<Func<ReferenceChecker, TSource, TDestination>> CreateAdaptFunc()
        {
            var checker = Expression.Parameter(typeof (ReferenceChecker));
            var p = Expression.Parameter(typeof (TSource));
            var body = CreateExpressionBody(p);
            return Expression.Lambda<Func<ReferenceChecker, TSource, TDestination>>(body, checker, p);
        }

        public static Expression<Func<ReferenceChecker, TSource, TDestination, TDestination>> CreateAdaptTargetFunc()
        {
            var checker = Expression.Parameter(typeof(ReferenceChecker));
            var p = Expression.Parameter(typeof(TSource));
            var p2 = Expression.Parameter(typeof (TDestination));
            var body = CreateExpressionBody(p);
            return Expression.Lambda<Func<ReferenceChecker, TSource, TDestination, TDestination>>(body, checker, p, p2);
        }

        private static Expression CreateExpressionBody(ParameterExpression p)
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
                convert = ReflectionUtils.BuildUnderlyingTypeConvertExpression<TSource, TDestination>(convert);
                if (convert.Type != destinationType)
                    convert = Expression.Convert(convert, destinationType);
            }
            if ((!sourceType.IsValueType || sourceType.IsNullable()) && destinationType.IsValueType && !destinationType.IsNullable())
            {
                var compareNull = Expression.Equal(p, Expression.Constant(null, sourceType));
                convert = Expression.Condition(compareNull, Expression.Constant(default(TDestination), typeof(TDestination)), convert);
            }

            list.Add(Expression.Assign(pDest, convert));

            var destinationTransforms = TypeAdapterConfig.GlobalSettings.DestinationTransforms.Transforms;
            if (destinationTransforms.ContainsKey(destinationType))
            {
                var transform = destinationTransforms[destinationType];
                var invoke = Expression.Invoke(transform, pDest);
                list.Add(Expression.Assign(pDest, invoke));
            }
            var setting = TypeAdapterConfig<TSource, TDestination>.ConfigSettings;
            var localTransform = setting?.DestinationTransforms.Transforms;
            if (localTransform != null && localTransform.ContainsKey(destinationType))
            {
                var transform = localTransform[destinationType];
                var invoke = Expression.Invoke(transform, pDest);
                list.Add(Expression.Assign(pDest, invoke));
            }

            list.Add(pDest);
            return Expression.Block(destinationType, new[] {pDest}, list);
        }
    }
}
