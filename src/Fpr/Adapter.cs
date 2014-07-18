using System;

namespace Fpr
{
    public interface IAdapter
    {
        TDestination Adapt<TDestination>(object source);
        TDestination Adapt<TSource, TDestination>(TSource source);
        TDestination Adapt<TSource, TDestination>(TSource source, TDestination destination);
        object Adapt(object source, Type sourceType, Type destinationType);
        object Adapt(object source, object destination, Type sourceType, Type destinationType);
    }

    public class Adapter : IAdapter
    {
        public TDestination Adapt<TDestination>(object source)
        {
            return TypeAdapter.Adapt<TDestination>(source);
        }

        public TDestination Adapt<TSource, TDestination>(TSource source)
        {
            return TypeAdapter.Adapt<TSource, TDestination>(source);
        }

        public TDestination Adapt<TSource, TDestination>(TSource source, TDestination destination)
        {
            return TypeAdapter.Adapt(source, destination);
        }

        public object Adapt(object source, Type sourceType, Type destinationType)
        {
            return TypeAdapter.Adapt(source, sourceType, destinationType);
        }

        public object Adapt(object source, object destination, Type sourceType, Type destinationType)
        {
            return TypeAdapter.Adapt(source, destination, sourceType, destinationType);
        } 
    }

}