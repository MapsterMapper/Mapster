using Mapster.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace Mapster
{
    public class TypeAdapterBuilder<TSource> : ITypeAdapterBuilder<TSource>
    {
        TSource Source { get; }
        TSource IAdapterBuilder<TSource>.Source => Source;
        TypeAdapterConfig Config { get; set; }
        TypeAdapterConfig IAdapterBuilder.Config => Config;

        private Dictionary<string, object>? _parameters;
        Dictionary<string, object> Parameters => _parameters ??= new Dictionary<string, object>();
        Dictionary<string, object> IAdapterBuilder.Parameters => Parameters;
        bool IAdapterBuilder.HasParameter => _parameters != null && _parameters.Count > 0;

        internal TypeAdapterBuilder(TSource source, TypeAdapterConfig config)
        {
            Source = source;
            Config = config;
        }

        [SuppressMessage("ReSharper", "ExplicitCallerInfoArgument")]
        public ITypeAdapterBuilder<TSource> ForkConfig(Action<TypeAdapterConfig> action,
#if !NET40
            [CallerFilePath]
#endif
            string key1 = "",
#if !NET40
            [CallerLineNumber]
#endif
            int key2 = 0)
        {
            Config = Config.Fork(action, key1, key2);
            return this;
        }

        public ITypeAdapterBuilder<TSource> AddParameters(string name, object value)
        {
            Parameters.Add(name, value);
            return this;
        }

        private MapContextScope CreateMapContextScope()
        {
            var scope = new MapContextScope();
            var parameters = scope.Context.Parameters;
            foreach (var kvp in Parameters)
            {
                parameters[kvp.Key] = kvp.Value;
            }

            return scope;
        }

        MapContextScope IAdapterBuilder.CreateMapContextScope() => CreateMapContextScope();

        public TDestination AdaptToType<TDestination>()
        {
            if (_parameters == null)
                return Map<TDestination>();

            using (CreateMapContextScope())
            {
                return Map<TDestination>();
            }
        }

        private TDestination Map<TDestination>()
        {
            var fn = Config.GetMapFunction<TSource, TDestination>();
            return fn(Source);
        }

        public TDestination AdaptTo<TDestination>(TDestination destination)
        {
            if (_parameters == null)
                return MapToTarget(destination);

            using (CreateMapContextScope())
            {
                return MapToTarget(destination);
            }
        }

        private TDestination MapToTarget<TDestination>(TDestination destination)
        {
            var fn = Config.GetMapToTargetFunction<TSource, TDestination>();
            return fn(Source, destination);
        }

        public Expression<Func<TSource, TDestination>> CreateMapExpression<TDestination>()
        {
            var tuple = new TypeTuple(typeof(TSource), typeof(TDestination));
            return (Expression<Func<TSource, TDestination>>) Config.CreateMapExpression(tuple, MapType.Map);
        }

        public Expression<Func<TSource, TDestination, TDestination>> CreateMapToTargetExpression<TDestination>()
        {
            var tuple = new TypeTuple(typeof(TSource), typeof(TDestination));
            return (Expression<Func<TSource, TDestination, TDestination>>) Config.CreateMapExpression(tuple, MapType.MapToTarget);
        }

        public Expression<Func<TSource, TDestination>> CreateProjectionExpression<TDestination>()
        {
            var tuple = new TypeTuple(typeof(TSource), typeof(TDestination));
            return (Expression<Func<TSource, TDestination>>) Config.CreateMapExpression(tuple, MapType.Projection);
        }
    }
}
