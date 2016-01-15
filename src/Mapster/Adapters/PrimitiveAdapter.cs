using System;
using System.Linq.Expressions;
using Mapster.Utils;

namespace Mapster.Adapters
{
    internal class PrimitiveAdapter : BaseInlineAdapter
    {
        public override bool CanAdapt(Type sourceType, Type destinationType)
        {
            return true;
        }

        public override Expression CreateExpression(Expression source, Expression destination, Type destinationType)
        {
            Expression convert = source;
            var sourceType = source.Type;
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
                var compareNull = Expression.Equal(source, Expression.Constant(null, sourceType));
                convert = Expression.Condition(compareNull, Expression.Constant(destinationType.GetDefault(), destinationType), convert);
            }

            var destinationTransforms = BaseTypeAdapterConfig.GlobalSettings.DestinationTransforms.Transforms;
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
