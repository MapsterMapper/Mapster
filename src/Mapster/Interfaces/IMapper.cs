using System;
using Mapster;

namespace MapsterMapper
{
    public interface IMapper
    {
        TypeAdapterConfig Config { get; }
        TypeAdapterBuilder<TSource> From<TSource>(TSource source);
        TDestination Map<TDestination>(object source);
        TDestination Map<TSource, TDestination>(TSource source);
        TDestination Map<TSource, TDestination>(TSource source, TDestination destination);
        object Map(object source, Type sourceType, Type destinationType);
        object Map(object source, object destination, Type sourceType, Type destinationType);
    }
}
