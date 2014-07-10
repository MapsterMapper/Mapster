using System;
using System.Collections.Generic;
using Fpr.Utils;

namespace Fpr.Adapters
{
    public class PrimitiveAdapter<TSource, TDestination>
    {
        public static readonly FastInvokeHandler _converter = CreateConverter();

        public static TDestination Adapt(TSource source, TDestination destination)
        {
            TDestination destinationValue;

            if (source == null)
                destinationValue = default(TDestination);
            else if (_converter == null)
                destinationValue = (TDestination)Convert.ChangeType(source, typeof(TDestination));
            else
                destinationValue = (TDestination)_converter(null, new object[] { source });

            IDictionary<Type,Func<object, object>> transforms = TypeAdapterConfig.GlobalSettings.DestinationTransforms.Transforms;
            Type destinationType = typeof (TDestination);
            if (transforms.Count > 0 && transforms.ContainsKey(destinationType))
            {
                return (TDestination)transforms[destinationType](destinationValue);
            }

            return destinationValue;
        }

        public static TDestination Adapt(TSource source)
        {
            return Adapt(source, default(TDestination));
        }

        private static FastInvokeHandler CreateConverter()
        {
            return ReflectionUtils.CreatePrimitiveConverter(typeof(TSource), typeof(TDestination));
        }

    }
}
