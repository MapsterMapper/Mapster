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
        public Adapter(TypeAdapterConfig config)
        {
            _config = config;
        }

        public TDestination Adapt<TDestination>(object source)
        {
            var fn = _config.GetMapFunction(source.GetType(), typeof(TDestination));
            var result = (TDestination)fn.DynamicInvoke(source);
            MapContext.Clear();
            return result;
        }

        public TDestination Adapt<TSource, TDestination>(TSource source)
        {
            var result = _config.GetMapFunction<TSource, TDestination>()(source);
            MapContext.Clear();
            return result;
        }

        public TDestination Adapt<TSource, TDestination>(TSource source, TDestination destination)
        {
            var result = _config.GetMapToTargetFunction<TSource, TDestination>()(source, destination);
            MapContext.Clear();
            return result;
        }

        public object Adapt(object source, Type sourceType, Type destinationType)
        {
            var fn = _config.GetMapFunction(sourceType, destinationType);
            var result = fn.DynamicInvoke(source);
            MapContext.Clear();
            return result;
        }

        public object Adapt(object source, object destination, Type sourceType, Type destinationType)
        {
            var fn = _config.GetMapToTargetFunction(sourceType, destinationType);
            var result = fn.DynamicInvoke(source, destination);
            MapContext.Clear();
            return result;
        } 
    }

}