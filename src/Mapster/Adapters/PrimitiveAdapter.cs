using System;
using System.Linq.Expressions;
using Mapster.Utils;

namespace Mapster.Adapters
{
    internal class PrimitiveAdapter : ITypeAdapterWithTarget, ITypeExpression
    {
        public bool CanAdapt(Type sourceType, Type destinationType)
        {
            return true;
        }

        public Func<MapContext, TSource, TDestination> CreateAdaptFunc<TSource, TDestination>()
        {
            //var depth = Expression.Parameter(typeof (int));
            var context = Expression.Parameter(typeof (MapContext));
            var p = Expression.Parameter(typeof (TSource));
            var body = CreateExpression(p, typeof(TSource), typeof(TDestination));
            return Expression.Lambda<Func<MapContext, TSource, TDestination>>(body, context, p).Compile();
        }

        public Func<MapContext, TSource, TDestination, TDestination> CreateAdaptTargetFunc<TSource, TDestination>()
        {
            //var depth = Expression.Parameter(typeof(int));
            var context = Expression.Parameter(typeof(MapContext));
            var p = Expression.Parameter(typeof(TSource));
            var p2 = Expression.Parameter(typeof (TDestination));
            var body = CreateExpression(p, typeof(TSource), typeof(TDestination));
            return Expression.Lambda<Func<MapContext, TSource, TDestination, TDestination>>(body, context, p, p2).Compile();
        }

        public Expression CreateExpression(Expression p, Type sourceType, Type destinationType)
        {
            Expression convert = p;
            if (sourceType != destinationType)
            {
                if (sourceType.IsNullable())
                {
                    convert = Expression.Convert(convert, sourceType.GetGenericArguments()[0]);
                }
                convert = ReflectionUtils.BuildUnderlyingTypeConvertExpression(convert, sourceType, destinationType);
                if (convert.Type != destinationType)
                    convert = Expression.Convert(convert, destinationType);
            }
            if ((!sourceType.IsValueType || sourceType.IsNullable()) && destinationType.IsValueType && !destinationType.IsNullable())
            {
                var compareNull = Expression.Equal(p, Expression.Constant(null, sourceType));
                convert = Expression.Condition(compareNull, Expression.Constant(destinationType.GetDefault(), destinationType), convert);
            }

            var destinationTransforms = TypeAdapterConfig.GlobalSettings.DestinationTransforms.Transforms;
            if (destinationTransforms.ContainsKey(destinationType))
            {
                var transform = destinationTransforms[destinationType];
                convert = Expression.Invoke(transform, convert);
            }
            var settings = TypeAdapter.GetSettings(sourceType, destinationType);
            var localTransform = settings?.DestinationTransforms.Transforms;
            if (localTransform != null && localTransform.ContainsKey(destinationType))
            {
                var transform = localTransform[destinationType];
                convert = Expression.Invoke(transform, convert);
            }

            return convert;
        }
    }
}
