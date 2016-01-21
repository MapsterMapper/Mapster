using System;

namespace Mapster
{
    public static class TypeAdapter
    {
        /// <summary>
        /// Adapte the source object to the destination type.
        /// </summary>
        /// <typeparam name="TDestination">Destination type.</typeparam>
        /// <param name="source">Source object to adapt.</param>
        /// <param name="config">Configuration</param>
        /// <returns>Adapted destination type.</returns>
        public static TDestination Adapt<TDestination>(this object source, TypeAdapterConfig config = null)
        {
            config = config ?? TypeAdapterConfig.GlobalSettings;
            dynamic fn = config.GetMapFunction(source.GetType(), typeof(TDestination));
            try
            {
                return (TDestination)fn((dynamic) source);
            }
            finally
            {
                MapContext.Clear();
            }
        }

        /// <summary>
        /// Adapte the source object to the destination type.
        /// </summary>
        /// <typeparam name="TSource">Source type.</typeparam>
        /// <typeparam name="TDestination">Destination type.</typeparam>
        /// <param name="source">Source object to adapt.</param>
        /// <returns>Adapted destination type.</returns>
        public static TDestination Adapt<TSource, TDestination>(this TSource source)
        {
            try
            {
                return TypeAdapter<TSource, TDestination>.Map(source);
            }
            finally
            {
                MapContext.Clear();
            }
        }

        /// <summary>
        /// Adapte the source object to the destination type.
        /// </summary>
        /// <typeparam name="TSource">Source type.</typeparam>
        /// <typeparam name="TDestination">Destination type.</typeparam>
        /// <param name="source">Source object to adapt.</param>
        /// <param name="config">Configuration</param>
        /// <returns>Adapted destination type.</returns>
        public static TDestination Adapt<TSource, TDestination>(this TSource source, TypeAdapterConfig config)
        {
            var fn = config.GetMapFunction<TSource, TDestination>();
            try
            {
                return fn(source);
            }
            finally
            {
                MapContext.Clear();
            }
        }

        /// <summary>
        /// Adapte the source object to the existing destination object.
        /// </summary>
        /// <typeparam name="TSource">Source type.</typeparam>
        /// <typeparam name="TDestination">Destination type.</typeparam>
        /// <param name="source">Source object to adapt.</param>
        /// <param name="destination">The destination object to populate.</param>
        /// <param name="config">Configuration</param>
        /// <returns>Adapted destination type.</returns>
        public static TDestination Adapt<TSource, TDestination>(this TSource source, TDestination destination, TypeAdapterConfig config = null)
        {
            config = config ?? TypeAdapterConfig.GlobalSettings;
            var fn = config.GetMapToTargetFunction<TSource, TDestination>();

            try
            {
                return fn(source, destination);
            }
            finally
            {
                MapContext.Clear();
            }
        }

        /// <summary>
        /// Adapte the source object to the destination type.
        /// </summary>
        /// <param name="source">Source object to adapt.</param>
        /// <param name="sourceType">The type of the source object.</param>
        /// <param name="destinationType">The type of the destination object.</param>
        /// <param name="config">Configuration</param>
        /// <returns>Adapted destination type.</returns>
        public static object Adapt(this object source, Type sourceType, Type destinationType, TypeAdapterConfig config = null)
        {
            config = config ?? TypeAdapterConfig.GlobalSettings;
            dynamic fn = config.GetMapFunction(sourceType, destinationType);
            try
            {
                return fn((dynamic) source);
            }
            finally
            {
                MapContext.Clear();
            }
        }

        /// <summary>
        /// Adapte the source object to an existing destination object.
        /// </summary>
        /// <param name="source">Source object to adapt.</param>
        /// <param name="destination">Destination object to populate.</param>
        /// <param name="sourceType">The type of the source object.</param>
        /// <param name="destinationType">The type of the destination object.</param>
        /// <param name="config">Configuration</param>
        /// <returns>Adapted destination type.</returns>
        public static object Adapt(this object source, object destination, Type sourceType, Type destinationType, TypeAdapterConfig config = null)
        {
            config = config ?? TypeAdapterConfig.GlobalSettings;
            dynamic fn = config.GetMapToTargetFunction(sourceType, destinationType);
            try
            {
                return fn((dynamic) source, (dynamic) destination);
            }
            finally
            {
                MapContext.Clear();
            }
        }

        /// <summary>
        /// Returns an instance representation of the adapter, mainly for DI/IOC situations.
        /// </summary>
        /// <param name="config">Configuration</param>
        /// <returns>Instance of the adapter.</returns>
        public static IAdapter GetInstance(TypeAdapterConfig config = null)
        {
            return new Adapter(config ?? TypeAdapterConfig.GlobalSettings);
        }
    }

    internal static class TypeAdapter<TSource, TDestination>
    {
        public static Func<TSource, TDestination> Map = TypeAdapterConfig.GlobalSettings.GetMapFunction<TSource, TDestination>();
    }
}
