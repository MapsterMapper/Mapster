using System;

namespace Mapster
{
    [Obsolete("Please use IMapper instead")]
    public interface IAdapter
    {
        TypeAdapterBuilder<TSource> BuildAdapter<TSource>(TSource source);
        TDestination Adapt<TDestination>(object source);
        TDestination Adapt<TSource, TDestination>(TSource source);
        TDestination Adapt<TSource, TDestination>(TSource source, TDestination destination);
        object Adapt(object source, Type sourceType, Type destinationType);
        object Adapt(object source, object destination, Type sourceType, Type destinationType);
    }

}