using System;
using System.Collections.Generic;

namespace Mapster
{
    public static class TypeAdapter
    {
        public static TypeAdapter<TSource> BuildAdapter<TSource>(this TSource source)
        {
            return new TypeAdapter<TSource>(source);
        }

        /// <summary>
        /// Adapt the source object to the destination type.
        /// </summary>
        /// <typeparam name="TDestination">Destination type.</typeparam>
        /// <param name="source">Source object to adapt.</param>
        /// <returns>Adapted destination type.</returns>
        public static TDestination Adapt<TDestination>(this object source)
        {
            return Adapt<TDestination>(source, TypeAdapterConfig.GlobalSettings);
        }

        /// <summary>
        /// Adapt the source object to the destination type.
        /// </summary>
        /// <typeparam name="TDestination">Destination type.</typeparam>
        /// <param name="source">Source object to adapt.</param>
        /// <param name="config">Configuration</param>
        /// <returns>Adapted destination type.</returns>
        public static TDestination Adapt<TDestination>(this object source, TypeAdapterConfig config)
        {
            if (source == null)
                return default(TDestination);
            dynamic fn = config.GetMapFunction(source.GetType(), typeof(TDestination));
            return (TDestination)fn((dynamic)source);
        }

        /// <summary>
        /// Adapt the source object to the destination type.
        /// </summary>
        /// <typeparam name="TSource">Source type.</typeparam>
        /// <typeparam name="TDestination">Destination type.</typeparam>
        /// <param name="source">Source object to adapt.</param>
        /// <returns>Adapted destination type.</returns>
        public static TDestination Adapt<TSource, TDestination>(this TSource source)
        {
            return TypeAdapter<TSource, TDestination>.Map(source);
        }

        /// <summary>
        /// Adapt the source object to the destination type.
        /// </summary>
        /// <typeparam name="TSource">Source type.</typeparam>
        /// <typeparam name="TDestination">Destination type.</typeparam>
        /// <param name="source">Source object to adapt.</param>
        /// <param name="config">Configuration</param>
        /// <returns>Adapted destination type.</returns>
        public static TDestination Adapt<TSource, TDestination>(this TSource source, TypeAdapterConfig config)
        {
            var fn = config.GetMapFunction<TSource, TDestination>();
            return fn(source);
        }

        /// <summary>
        /// Adapt the source object to the existing destination object.
        /// </summary>
        /// <typeparam name="TSource">Source type.</typeparam>
        /// <typeparam name="TDestination">Destination type.</typeparam>
        /// <param name="source">Source object to adapt.</param>
        /// <param name="destination">The destination object to populate.</param>
        /// <returns>Adapted destination type.</returns>
        public static TDestination Adapt<TSource, TDestination>(this TSource source, TDestination destination)
        {
            return Adapt(source, destination, TypeAdapterConfig.GlobalSettings);
        }

        /// <summary>
        /// Adapt the source object to the existing destination object.
        /// </summary>
        /// <typeparam name="TSource">Source type.</typeparam>
        /// <typeparam name="TDestination">Destination type.</typeparam>
        /// <param name="source">Source object to adapt.</param>
        /// <param name="destination">The destination object to populate.</param>
        /// <param name="config">Configuration</param>
        /// <returns>Adapted destination type.</returns>
        public static TDestination Adapt<TSource, TDestination>(this TSource source, TDestination destination, TypeAdapterConfig config)
        {
            var fn = config.GetMapToTargetFunction<TSource, TDestination>();
            return fn(source, destination);
        }

        /// <summary>
        /// Adapt the source object to the destination type.
        /// </summary>
        /// <param name="source">Source object to adapt.</param>
        /// <param name="sourceType">The type of the source object.</param>
        /// <param name="destinationType">The type of the destination object.</param>
        /// <returns>Adapted destination type.</returns>
        public static object Adapt(this object source, Type sourceType, Type destinationType)
        {
            return Adapt(source, sourceType, destinationType, TypeAdapterConfig.GlobalSettings);
        }

        /// <summary>
        /// Adapt the source object to the destination type.
        /// </summary>
        /// <param name="source">Source object to adapt.</param>
        /// <param name="sourceType">The type of the source object.</param>
        /// <param name="destinationType">The type of the destination object.</param>
        /// <param name="config">Configuration</param>
        /// <returns>Adapted destination type.</returns>
        public static object Adapt(this object source, Type sourceType, Type destinationType, TypeAdapterConfig config)
        {
            dynamic fn = config.GetMapFunction(sourceType, destinationType);
            return fn((dynamic)source);
        }

        /// <summary>
        /// Adapt the source object to an existing destination object.
        /// </summary>
        /// <param name="source">Source object to adapt.</param>
        /// <param name="destination">Destination object to populate.</param>
        /// <param name="sourceType">The type of the source object.</param>
        /// <param name="destinationType">The type of the destination object.</param>
        /// <returns>Adapted destination type.</returns>
        public static object Adapt(this object source, object destination, Type sourceType, Type destinationType)
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
        public static object Adapt(this object source, object destination, Type sourceType, Type destinationType, TypeAdapterConfig config)
        {
            dynamic fn = config.GetMapToTargetFunction(sourceType, destinationType);
            return fn((dynamic)source, (dynamic)destination);
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

    public class TypeAdapter<TSource>
    {
        TSource Source { get; }
        TypeAdapterConfig Config { get; set; }

        private Dictionary<string, object> _parameters;
        Dictionary<string, object> Parameters
        {
            get { return _parameters ?? (_parameters = new Dictionary<string, object>(ReferenceComparer.Default)); }
        }

        public TypeAdapter(TSource source)
        {
            this.Source = source;
            this.Config = TypeAdapterConfig.GlobalSettings;
        }

        public TypeAdapter<TSource> UseConfig(TypeAdapterConfig config)
        {
            this.Config = config;
            return this;
        }

        public TypeAdapter<TSource> AddParameters(string name, object value)
        {
            this.Parameters.Add(name, value);
            return this;
        }

        public TDestination AdaptToType<TDestination>()
        {
            if (_parameters == null)
                return Map<TDestination>();

            using (var scope = new MapContextScope())
            {
                var parameters = scope.Context.Parameters;
                foreach (var kvp in _parameters)
                {
                    parameters[kvp.Key] = kvp.Value;
                }
                return Map<TDestination>();
            }
        }

        private TDestination Map<TDestination>()
        {
            var fn = this.Config.GetMapFunction<TSource, TDestination>();
            return fn(this.Source);
        }

        public TDestination AdaptTo<TDestination>(TDestination destination)
        {
            if (_parameters == null)
                return MapToTarget(destination);

            using (var scope = new MapContextScope())
            {
                var parameters = scope.Context.Parameters;
                foreach (var kvp in _parameters)
                {
                    parameters[kvp.Key] = kvp.Value;
                }
                return MapToTarget(destination);
            }
        }

        private TDestination MapToTarget<TDestination>(TDestination destination)
        {
            var fn = this.Config.GetMapToTargetFunction<TSource, TDestination>();
            return fn(this.Source, destination);
        }
    }
}
