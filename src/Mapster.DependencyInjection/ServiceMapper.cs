using System;
using Mapster;

namespace MapsterMapper
{
    public class ServiceMapper : Mapper
    {
        internal const string DI_KEY = "Mapster.DependencyInjection.sp";
        private readonly IServiceProvider _serviceProvider;

        public ServiceMapper(IServiceProvider serviceProvider, TypeAdapterConfig config) : base(config)
        {
            _serviceProvider = serviceProvider;
        }

		/// <summary>
		/// Create mapping builder.
		/// </summary>
		/// <typeparam name="TSource">Source type to create mapping builder.</typeparam>
		/// <param name="source">Source object to create mapping builder.</param>
		/// <returns></returns>
		public override ITypeAdapterBuilder<TSource> From<TSource>(TSource source)
        {
            return base.From(source)
                .AddParameters(DI_KEY, _serviceProvider);
        }


		/// <summary>
		/// Perform mapping from source object to type of destination.
		/// </summary>
		/// <typeparam name="TDestination">Destination type to create mapping builder.</typeparam>
		/// <param name="source">Source object to create mapping builder.</param>
		/// <returns>Type of destination object that mapped.</returns>
		public override TDestination Map<TDestination>(object source)
        {
            using var scope = new MapContextScope();
            scope.Context.Parameters[DI_KEY] = _serviceProvider;
            return base.Map<TDestination>(source);
        }


		/// <summary>
		/// Perform mapping from type of source to type of destination.
		/// </summary>
		/// <typeparam name="TSource">Source type to map.</typeparam>
		/// <typeparam name="TDestination">Destination type to map.</typeparam>
		/// <param name="source">Source object to map.</param>
		/// <returns>Type of destination object that mapped.</returns>
		public override TDestination Map<TSource, TDestination>(TSource source)
        {
            using var scope = new MapContextScope();
            scope.Context.Parameters[DI_KEY] = _serviceProvider;
            return base.Map<TSource, TDestination>(source);
        }


		/// <summary>
		/// Perform mapping from type of source to type of destination.
		/// </summary>
		/// <typeparam name="TSource">Source type to map.</typeparam>
		/// <typeparam name="TDestination">Destination type to map.</typeparam>
		/// <param name="source">Source object to map.</param>
		/// <param name="destination">Destination object to map.</param>
		/// <returns>Type of destination object that mapped.</returns>
		public override TDestination Map<TSource, TDestination>(TSource source, TDestination destination)
        {
            using var scope = new MapContextScope();
            scope.Context.Parameters[DI_KEY] = _serviceProvider;
            return base.Map(source, destination);
        }


		/// <summary>
		/// Perform mapping source object from source type to destination type.
		/// </summary>
		/// <param name="source">Source object to map.</param>
		/// <param name="sourceType">Source type to map.</param>
		/// <param name="destinationType">Destination type to map.</param>
		/// <returns>Mapped object.</returns>
		public override object Map(object source, Type sourceType, Type destinationType)
        {
            using var scope = new MapContextScope();
            scope.Context.Parameters[DI_KEY] = _serviceProvider;
            return base.Map(source, sourceType, destinationType);
        }


		/// <summary>
		/// Perform mapping source object from source type to destination type.
		/// </summary>
		/// <param name="source">Source object to map.</param>
		/// <param name="destination">Destination object to map.</param>
		/// <param name="sourceType">Source type to map.</param>
		/// <param name="destinationType">Destination type to map.</param>
		/// <returns></returns>
		public override object Map(object source, object destination, Type sourceType, Type destinationType)
        {
            using var scope = new MapContextScope();
            scope.Context.Parameters[DI_KEY] = _serviceProvider;
            return base.Map(source, destination, sourceType, destinationType);
        }
    }
}
