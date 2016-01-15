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
        /// <returns>Adapted destination type.</returns>
        public static TDestination Adapt<TDestination>(object source, TypeAdapterConfig config = null)
        {
            config = config ?? TypeAdapterConfig.GlobalSettings;
            var fn = config.GetMapFunction(source.GetType(), typeof(TDestination));
            var result = (TDestination)fn.DynamicInvoke(source);
            MapContext.Clear();
            return result;
        }

        /// <summary>
        /// Adapte the source object to the destination type.
        /// </summary>
        /// <typeparam name="TSource">Source type.</typeparam>
        /// <typeparam name="TDestination">Destination type.</typeparam>
        /// <param name="source">Source object to adapt.</param>
        /// <returns>Adapted destination type.</returns>
        public static TDestination Adapt<TSource, TDestination>(TSource source, TypeAdapterConfig config = null)
        {
            config = config ?? TypeAdapterConfig.GlobalSettings;
            var result = config.GetMapFunction<TSource, TDestination>()(source);
            MapContext.Clear();
            return result;
        }

        /// <summary>
        /// Adapte the source object to the existing destination object.
        /// </summary>
        /// <typeparam name="TSource">Source type.</typeparam>
        /// <typeparam name="TDestination">Destination type.</typeparam>
        /// <param name="source">Source object to adapt.</param>
        /// <param name="destination">The destination object to populate.</param>
        /// <returns>Adapted destination type.</returns>
        public static TDestination Adapt<TSource, TDestination>(TSource source, TDestination destination, TypeAdapterConfig config = null)
        {
            config = config ?? TypeAdapterConfig.GlobalSettings;
            var result = config.GetMapToTargetFunction<TSource, TDestination>()(source, destination);
            MapContext.Clear();
            return result;
        }

        /// <summary>
        /// Adapte the source object to the destination type.
        /// </summary>
        /// <param name="source">Source object to adapt.</param>
        /// <param name="sourceType">The type of the source object.</param>
        /// <param name="destinationType">The type of the destination object.</param>
        /// <returns>Adapted destination type.</returns>
        public static object Adapt(object source, Type sourceType, Type destinationType, TypeAdapterConfig config = null)
        {
            config = config ?? TypeAdapterConfig.GlobalSettings;
            var fn = config.GetMapFunction(sourceType, destinationType);
            var result = fn.DynamicInvoke(source);
            MapContext.Clear();
            return result;
        }

        /// <summary>
        /// Adapte the source object to an existing destination object.
        /// </summary>
        /// <param name="source">Source object to adapt.</param>
        /// <param name="destination">Destination object to populate.</param>
        /// <param name="sourceType">The type of the source object.</param>
        /// <param name="destinationType">The type of the destination object.</param>
        /// <returns>Adapted destination type.</returns>
        public static object Adapt(object source, object destination, Type sourceType, Type destinationType, TypeAdapterConfig config = null)
        {
            config = config ?? TypeAdapterConfig.GlobalSettings;
            var fn = config.GetMapToTargetFunction(sourceType, destinationType);
            var result = fn.DynamicInvoke(source, destination);
            MapContext.Clear();
            return result;
        }

        /// <summary>
        /// Returns an instance representation of the adapter, mainly for DI/IOC situations.
        /// </summary>
        /// <returns>Instance of the adapter.</returns>
        public static IAdapter GetInstance(TypeAdapterConfig config = null)
        {
            return new Adapter(config ?? TypeAdapterConfig.GlobalSettings);
        }
    }
}
