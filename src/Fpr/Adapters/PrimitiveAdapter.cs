using System;
using System.Collections.Generic;
using Fpr.Utils;

namespace Fpr.Adapters
{
    public class PrimitiveAdapter<TSource, TDestination>
    {
        public static readonly FastInvokeHandler _converter = CreateConverter();
        public static Func<object, object> _transform;

        public static TDestination Adapt(TSource source, TDestination destination)
        {
            TDestination destinationValue;

            if (source == null)
                destinationValue = default(TDestination);
            else if (_converter == null)
                destinationValue = (TDestination)Convert.ChangeType(source, typeof(TDestination));
            else
                destinationValue = (TDestination)_converter(null, new object[] { source });

            if (_transform != null)
                return (TDestination)_transform(destinationValue);

            return destinationValue;
        }

        public static TDestination Adapt(TSource source)
        {
            return Adapt(source, default(TDestination));
        }

        private static FastInvokeHandler CreateConverter()
        {
            Type destinationType = typeof(TDestination);
            if (TypeAdapterConfig.GlobalSettings.DestinationTransforms.Transforms.ContainsKey(destinationType))
            {
                _transform = TypeAdapterConfig.GlobalSettings.DestinationTransforms.Transforms[destinationType];
            }

            return ReflectionUtils.CreatePrimitiveConverter(typeof(TSource), typeof(TDestination));
        }

    }
}
