using Mapster.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace Mapster
{
    public class TypeAdapterBuilder<TSource> : IAdapterBuilder<TSource>
    {
        TSource Source { get; }
        TSource IAdapterBuilder<TSource>.Source => this.Source;
        TypeAdapterConfig Config { get; set; }
        TypeAdapterConfig IAdapterBuilder.Config => this.Config;

        private Dictionary<string, object>? _parameters;
        Dictionary<string, object> Parameters => _parameters ??= new Dictionary<string, object>();
        Dictionary<string, object> IAdapterBuilder.Parameters => this.Parameters;
        bool IAdapterBuilder.HasParameter => _parameters != null && _parameters.Count > 0;

        internal TypeAdapterBuilder(TSource source, TypeAdapterConfig config)
        {
            this.Source = source;
            this.Config = config;
        }

        [SuppressMessage("ReSharper", "ExplicitCallerInfoArgument")]
        public TypeAdapterBuilder<TSource> ForkConfig(Action<TypeAdapterConfig> action,
#if !NET40
            [CallerFilePath]
#endif
            string key1 = "",
#if !NET40
            [CallerLineNumber]
#endif
            int key2 = 0)
        {
            this.Config = this.Config.Fork(action, key1, key2);
            return this;
        }

        public TypeAdapterBuilder<TSource> AddParameters(string name, object value)
        {
            this.Parameters.Add(name, value);
            return this;
        }

        private MapContextScope CreateMapContextScope()
        {
            var scope = new MapContextScope();
            var parameters = scope.Context.Parameters;
            foreach (var kvp in this.Parameters)
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

            using (this.CreateMapContextScope())
            {
                return Map<TDestination>();
            }
        }

        private TDestination Map<TDestination>()
        {
            var fn = this.Config.GetMapFunction<TSource, TDestination>();
            return fn(this.Source);
        }

        public TDestination AdaptTo<TDestination>(TDestination destination)
        {
            if (_parameters == null)
                return MapToTarget(destination);

            using (this.CreateMapContextScope())
            {
                return MapToTarget(destination);
            }
        }

        private TDestination MapToTarget<TDestination>(TDestination destination)
        {
            var fn = this.Config.GetMapToTargetFunction<TSource, TDestination>();
            return fn(this.Source, destination);
        }

        public Expression<Func<TSource, TDestination>> CreateMapExpression<TDestination>()
        {
            var tuple = new TypeTuple(typeof(TSource), typeof(TDestination));
            return (Expression<Func<TSource, TDestination>>) this.Config.CreateMapExpression(tuple, MapType.Map);
        }

        public Expression<Func<TSource, TDestination, TDestination>> CreateMapToTargetExpression<TDestination>()
        {
            var tuple = new TypeTuple(typeof(TSource), typeof(TDestination));
            return (Expression<Func<TSource, TDestination, TDestination>>) this.Config.CreateMapExpression(tuple, MapType.MapToTarget);
        }

        public Expression<Func<TSource, TDestination>> CreateProjectionExpression<TDestination>()
        {
            var tuple = new TypeTuple(typeof(TSource), typeof(TDestination));
            return (Expression<Func<TSource, TDestination>>) this.Config.CreateMapExpression(tuple, MapType.Projection);
        }
    }
}