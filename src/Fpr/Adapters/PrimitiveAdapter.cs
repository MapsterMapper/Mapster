using System;
using Fpr.Utils;

namespace Fpr.Adapters
{
    public class PrimitiveAdapter<TSource, TDestination>
    {

        public static readonly FastInvokeHandler _converter = CreateConverter();


        public static TDestination Adapt(TSource source, TDestination destination)
        {
            if (source == null)
                return default(TDestination);

            if (_converter == null)
                return (TDestination)Convert.ChangeType(source, typeof(TDestination));

            return (TDestination)_converter(null, new object[] { source });
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
