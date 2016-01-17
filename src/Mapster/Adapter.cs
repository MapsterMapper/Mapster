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
            dynamic fn = _config.GetMapFunction(source.GetType(), typeof(TDestination));
            try
            {
                return fn((dynamic)source);
            }
            finally
            {
                MapContext.Clear();
            }
        }

        public TDestination Adapt<TSource, TDestination>(TSource source)
        {
            var fn = _config.GetMapFunction<TSource, TDestination>();
            try
            {
                return fn(source);
            }
            finally
            {
                MapContext.Clear();
            }
        }

        public TDestination Adapt<TSource, TDestination>(TSource source, TDestination destination)
        {
            var fn = _config.GetMapToTargetFunction<TSource, TDestination>();

            try
            {
                return fn(source, destination);
            }
            finally
            {
                MapContext.Clear();
            }
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
            dynamic fn = _config.GetMapFunction(sourceType, destinationType);
            try
            {
                return fn((dynamic)source);
            }
            finally
            {
                MapContext.Clear();
            }
        }
    }

}