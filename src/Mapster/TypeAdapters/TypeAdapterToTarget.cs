using System;
using System.Reflection;

namespace Mapster
{
    public static partial class TypeAdapter
    {
        /// <summary>
        /// Adapt the source object to the existing destination object.
        /// </summary>
        /// <typeparam name="TSource">Source type.</typeparam>
        /// <typeparam name="TDestination">Destination type.</typeparam>
        /// <param name="source">Source object to adapt.</param>
        /// <param name="destination">The destination object to populate.</param>
        /// <param name="config">Configuration</param>
        public static void Adapt<TSource, TDestination>(this TSource source, TDestination destination, TypeAdapterConfig? config = null) where TDestination : class 
        {
            if (typeof(TDestination).IsRecordType())
                throw new InvalidOperationException($"Type: {typeof(TDestination).Name} is Immutable Using AdaptToTarget");

            var sourceType = source?.GetType();
            var destinationType = destination?.GetType();

            if(config is null)
                config = TypeAdapterConfig.GlobalSettings; 

            if (sourceType == typeof(object)) // Infinity loop in ObjectAdapter if Runtime Type of source is Object 
                return;

            if (typeof(TSource) == typeof(object) || typeof(TDestination) == typeof(object))
                UpdateFuncFromPackedinObject(source, destination, config, sourceType, destinationType);

            var fn = config.GetMapToTargetFunction<TSource, TDestination>();
            fn(source, destination);
        }

        /// <summary>
        /// Adapt the source object to an existing destination object.
        /// </summary>
        /// <param name="source">Source object to adapt.</param>
        /// <param name="destination">Destination object to populate.</param>
        /// <param name="sourceType">The type of the source object.</param>
        /// <param name="destinationType">The type of the destination object.</param>
        /// <returns>Adapted destination type.</returns>
        public static object? Adapt(this object source, object destination, Type sourceType, Type destinationType)
        {
            return Adapt(source, destination, sourceType, destinationType, TypeAdapterConfig.GlobalSettings);
        }

        /// <summary>
        /// Adapt the source object to an existing destination object.
        /// </summary>
        /// <param name="source">Source object to adapt.</param>
        /// <param name="destination">Destination object to populate.</param>
        /// <param name="sourceType">The type of the source object.</param>
        /// <param name="destinationType">The type of the destination object.</param>
        /// <param name="config">Configuration</param>
        /// <returns>Adapted destination type.</returns>
        public static object? Adapt(this object source, object destination, Type sourceType, Type destinationType, TypeAdapterConfig config)
        {
            var del = config.GetMapToTargetFunction(sourceType, destinationType);
            if (sourceType.GetTypeInfo().IsVisible && destinationType.GetTypeInfo().IsVisible)
            {
                dynamic fn = del;
                return fn((dynamic)source, (dynamic)destination);
            }
            else
            {
                //NOTE: if type is non-public, we cannot use dynamic
                //DynamicInvoke is slow, but works with non-public
                return del.DynamicInvoke(source, destination);
            }
        }

        /// <summary>
        /// Adapt the source object to the Immutable type base object.
        /// </summary>
        /// <typeparam name="TSource">Source type.</typeparam>
        /// <typeparam name="TDestination">Destination type.</typeparam>
        /// <param name="source">Source object to adapt.</param>
        /// <param name="baseValue">Instance of Immutable type.</param>
        /// <param name="config">Configuration</param>
        /// <returns>Nondestructive mutation baseValue using tranformed from source object values</returns>
        public static TDestination AdaptToTarget<TSource, TDestination>(this TSource source, TDestination baseValue, TypeAdapterConfig? config = null)
        {
            var sourceType = source?.GetType();
            var destinationType = baseValue?.GetType();

            if (config is null)
                config = TypeAdapterConfig.GlobalSettings;

            if (sourceType == typeof(object)) // Infinity loop in ObjectAdapter if Runtime Type of source is Object 
                return baseValue;

            if (typeof(TSource) == typeof(object) || typeof(TDestination) == typeof(object))
                return UpdateFuncFromPackedinObject(source, baseValue, config, sourceType, destinationType);

            var fn = config.GetMapToTargetFunction<TSource, TDestination>();
            return fn(source, baseValue);
        }

        private static TDestination UpdateFuncFromPackedinObject<TSource, TDestination>(TSource source, TDestination destination, TypeAdapterConfig config, Type sourceType, Type destinationType)
        {
            dynamic del = config.GetMapToTargetFunction(sourceType, destinationType);


            if (sourceType.GetTypeInfo().IsVisible && destinationType.GetTypeInfo().IsVisible)
            {
                dynamic objfn = del;
                return objfn((dynamic)source, (dynamic)destination);
            }
            else
            {
                //NOTE: if type is non-public, we cannot use dynamic
                //DynamicInvoke is slow, but works with non-public
                return (TDestination)del.DynamicInvoke(source, destination);
            }
        }
    }
}
