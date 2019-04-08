using Mapster.Models;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace Mapster
{
    public class TypeAdapterBuilder<TSource>
    {
        TSource Source { get; }
        TypeAdapterConfig Config { get; set; }

        private Dictionary<string, object> _parameters;
        Dictionary<string, object> Parameters => _parameters ?? (_parameters = new Dictionary<string, object>());

        internal TypeAdapterBuilder(TSource source, TypeAdapterConfig config)
        {
            this.Source = source;
            this.Config = config;
        }

        public TypeAdapterBuilder<TSource> ForkConfig(Action<TypeAdapterConfig> action,
#if !NET40
            [CallerFilePath]
#endif
            string key1 = null,
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

        public TDestination AdaptToType<TDestination>()
        {
            if (_parameters == null)
                return Map<TDestination>();

            using (var scope = new MapContextScope())
            {
                var parameters = scope.Context.Parameters;
                foreach (var kvp in _parameters)
                {
                    parameters[kvp.Key] = kvp.Value;
                }
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

            using (var scope = new MapContextScope())
            {
                var parameters = scope.Context.Parameters;
                foreach (var kvp in _parameters)
                {
                    parameters[kvp.Key] = kvp.Value;
                }
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