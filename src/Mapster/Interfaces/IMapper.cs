using System;
using Mapster;

// ReSharper disable once CheckNamespace
namespace MapsterMapper
{
    public interface IMapper
    {
        TypeAdapterConfig Config { get; }


		/// <summary>
		/// Create mapping builder.
		/// </summary>
		/// <typeparam name="TSource">Source type to create mapping builder.</typeparam>
		/// <param name="source">Source object to create mapping builder.</param>
		/// <returns>Adapter builder type.</returns>
		ITypeAdapterBuilder<TSource> From<TSource>(TSource source);


		/// <summary>
		/// Perform mapping from source object to type of destination.
		/// </summary>
		/// <typeparam name="TDestination">Destination type to create mapping builder.</typeparam>
		/// <param name="source">Source object to create mapping builder.</param>
		/// <returns>Type of destination object that mapped.</returns>
		TDestination Map<TDestination>(object source);


		/// <summary>
		/// Perform mapping from type of source to type of destination.
		/// </summary>
		/// <typeparam name="TSource">Source type to map.</typeparam>
		/// <typeparam name="TDestination">Destination type to map.</typeparam>
		/// <param name="source">Source object to map.</param>
		/// <returns>Type of destination object that mapped.</returns>
		TDestination Map<TSource, TDestination>(TSource source);


		/// <summary>
		/// Perform mapping from type of source to type of destination.
		/// </summary>
		/// <typeparam name="TSource">Source type to map.</typeparam>
		/// <typeparam name="TDestination">Destination type to map.</typeparam>
		/// <param name="source">Source object to map.</param>
		/// <param name="destination">Destination object to map.</param>
		/// <returns>Type of destination object that mapped.</returns>
		TDestination Map<TSource, TDestination>(TSource source, TDestination destination);


		/// <summary>
		/// Perform mapping source object from source type to destination type.
		/// </summary>
		/// <param name="source">Source object to map.</param>
		/// <param name="sourceType">Source type to map.</param>
		/// <param name="destinationType">Destination type to map.</param>
		/// <returns>Mapped object.</returns>
		object Map(object source, Type sourceType, Type destinationType);


		/// <summary>
		/// Perform mapping source object from source type to destination type.
		/// </summary>
		/// <param name="source">Source object to map.</param>
		/// <param name="destination">Destination object to map.</param>
		/// <param name="sourceType">Source type to map.</param>
		/// <param name="destinationType">Destination type to map.</param>
		/// <returns>Mapped object.</returns>
		object Map(object source, object destination, Type sourceType, Type destinationType);
    }
}
