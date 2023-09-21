using System;
using System.Reflection;
using Mapster;

// ReSharper disable once CheckNamespace
namespace MapsterMapper
{
    public class Mapper : IMapper
    {
        public TypeAdapterConfig Config { get; }

        public Mapper() : this(TypeAdapterConfig.GlobalSettings) { }

        public Mapper(TypeAdapterConfig config)
        {
            Config = config;
        }

		/// <summary>
		/// Create mapping builder.
		/// </summary>
		/// <typeparam name="TSource">Source type to create mapping builder.</typeparam>
		/// <param name="source"></param>
		/// <returns></returns>
		public virtual ITypeAdapterBuilder<TSource> From<TSource>(TSource source)
        {
            return TypeAdapter.BuildAdapter(source, Config);
        }


		/// <summary>
		/// Perform mapping source object to type of destination.
		/// </summary>
		/// <typeparam name="TDestination">Destination type to perform mapping</typeparam>
		/// <param name="source">Source object to perform mapping.</param>
		/// <returns>type of destination mapping result.</returns>
		public virtual TDestination Map<TDestination>(object source)
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (source == null)
                return default!;
            var type = source.GetType();
            var fn = Config.GetDynamicMapFunction<TDestination>(type);
            return fn(source);
        }


		/// <summary>
		/// Perform mapping from type of source to type of destination.
		/// </summary>
		/// <typeparam name="TSource">Source type to map.</typeparam>
		/// <typeparam name="TDestination">Destination type to map.</typeparam>
		/// <param name="source"></param>
		/// <returns>type of destination mapping result</returns>
		public virtual TDestination Map<TSource, TDestination>(TSource source)
        {
            var fn = Config.GetMapFunction<TSource, TDestination>();
            return fn(source);
        }


		/// <summary>
		/// Perform mapping from type of source to type of destination.
		/// </summary>
		/// <typeparam name="TSource">Source type to map.</typeparam>
		/// <typeparam name="TDestination">Destination type to map.</typeparam>
		/// <param name="source">Source object to map.</param>
		/// <param name="destination">Destination type to map.</param>
		/// <returns>type of destination mapping result</returns>
		public virtual TDestination Map<TSource, TDestination>(TSource source, TDestination destination)
        {
            var fn = Config.GetMapToTargetFunction<TSource, TDestination>();
            return fn(source, destination);
        }


        /// <summary>
        /// Perform mapping source object from source type to destination type.
        /// </summary>
        /// <param name="source">Source object to map.</param>
        /// <param name="sourceType">Source type to map.</param>
        /// <param name="destinationType">Destination type to map.</param>
        /// <returns>mapped result object</returns>
        public virtual object Map(object source, Type sourceType, Type destinationType)
        {
            var del = Config.GetMapFunction(sourceType, destinationType);
            if (sourceType.GetTypeInfo().IsVisible && destinationType.GetTypeInfo().IsVisible)
            {
                dynamic fn = del;
                return fn((dynamic)source);
            }
            else
            {
                //NOTE: if type is non-public, we cannot use dynamic
                //DynamicInvoke is slow, but works with non-public
                return del.DynamicInvoke(source);
            }
        }


		/// <summary>
		/// Perform mapping source object to destination object from source type to destination type.
		/// </summary>
		/// <param name="source">Source object to map.</param>
		/// <param name="destination">Destination object to map.</param>
		/// <param name="sourceType">Source type to map.</param>
		/// <param name="destinationType">Destination type to map.</param>
		/// <returns>mapped result object</returns>
		public virtual object Map(object source, object destination, Type sourceType, Type destinationType)
        {
            var del = Config.GetMapToTargetFunction(sourceType, destinationType);
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
    }

}
