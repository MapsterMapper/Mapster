using System;

namespace Mapster
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
        readonly TypeAdapterConfig _config;

        public Adapter() : this(TypeAdapterConfig.GlobalSettings) { }

        public Adapter(TypeAdapterConfig config)
        {
            _config = config;
        }

        public TDestination Adapt<TDestination>(object source)
        {
            dynamic fn = _config.GetMapFunction(source.GetType(), typeof(TDestination));
            return (TDestination)fn((dynamic)source);
        }

        public TDestination Adapt<TSource, TDestination>(TSource source)
        {
            var fn = _config.GetMapFunction<TSource, TDestination>();
            return fn(source);
        }

        public TDestination Adapt<TSource, TDestination>(TSource source, TDestination destination)
        {
            var fn = _config.GetMapToTargetFunction<TSource, TDestination>();
            return fn(source, destination);
        }

        public object Adapt(object source, Type sourceType, Type destinationType)
        {
            var fn = _config.GetMapFunction(sourceType, destinationType);
            return fn.DynamicInvoke(source);
        }

        public object Adapt(object source, object destination, Type sourceType, Type destinationType)
        {
            dynamic fn = _config.GetMapFunction(sourceType, destinationType);
            return fn((dynamic)source);
        }
    }

}