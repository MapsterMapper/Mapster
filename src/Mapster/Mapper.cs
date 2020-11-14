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

        public virtual TypeAdapterBuilder<TSource> From<TSource>(TSource source)
        {
            return new TypeAdapterBuilder<TSource>(source, Config);
        }

        public virtual TDestination Map<TDestination>(object source)
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (source == null)
                return default!;
            var type = source.GetType();
            var fn = Config.GetDynamicMapFunction<TDestination>(type);
            return fn(source);
        }

        public virtual TDestination Map<TSource, TDestination>(TSource source)
        {
            var fn = Config.GetMapFunction<TSource, TDestination>();
            return fn(source);
        }

        public virtual TDestination Map<TSource, TDestination>(TSource source, TDestination destination)
        {
            var fn = Config.GetMapToTargetFunction<TSource, TDestination>();
            return fn(source, destination);
        }

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