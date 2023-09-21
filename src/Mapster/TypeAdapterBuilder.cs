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


		/// <summary>
		/// Allow you to keep config and mapping inline.
		/// </summary>
		/// <param name="action"></param>
		/// <param name="key1"></param>
		/// <param name="key2"></param>
		/// <returns></returns>
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


		/// <summary>
		/// Passing runtime value.
		/// </summary>
		/// <param name="name">Parameter name.</param>
		/// <param name="value">Parameter value</param>
		/// <returns></returns>
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
		/// <summary>
		/// Mapping to new type using in adapter builder scenario.
		/// </summary>
		/// <typeparam name="TDestination">Destination type to adopt.</typeparam>
		/// <returns></returns>
		public TDestination AdaptToType<TDestination>()
        {
            if (_parameters == null)
                return Map<TDestination>();

            using (CreateMapContextScope())
            {
                return Map<TDestination>();
            }
        }


		/// <summary>
		/// Perform mapping to type of destination in adapter builder scenario.
		/// </summary>
		/// <typeparam name="TDestination">Destination type to map.</typeparam>
		/// <returns></returns>
		private TDestination Map<TDestination>()
        {
            var fn = Config.GetMapFunction<TSource, TDestination>();
            return fn(Source);
        }


		/// <summary>
		/// Mapping to existing object in adapter builder scenario.
		/// </summary>
		/// <typeparam name="TDestination">Destination type to adopt.</typeparam>
		/// <param name="destination"></param>
		/// <returns></returns>
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


		/// <summary>
		/// Get mapping expression.
		/// </summary>
		/// <typeparam name="TDestination">Destination type to create map expression.</typeparam>
		/// <returns></returns>
		public Expression<Func<TSource, TDestination>> CreateMapExpression<TDestination>()
        {
            var tuple = new TypeTuple(typeof(TSource), typeof(TDestination));
            return (Expression<Func<TSource, TDestination>>) Config.CreateMapExpression(tuple, MapType.Map);
        }


		/// <summary>
		/// Get mapping to existing object expression.
		/// </summary>
		/// <typeparam name="TDestination">Destination type to create map to target expression.</typeparam>
		/// <returns></returns>
		public Expression<Func<TSource, TDestination, TDestination>> CreateMapToTargetExpression<TDestination>()
        {
            var tuple = new TypeTuple(typeof(TSource), typeof(TDestination));
            return (Expression<Func<TSource, TDestination, TDestination>>) Config.CreateMapExpression(tuple, MapType.MapToTarget);
        }


		/// <summary>
		/// Get mapping from queryable expression.
		/// </summary>
		/// <typeparam name="TDestination">Destination type to create projection expression.</typeparam>
		/// <returns></returns>
		public Expression<Func<TSource, TDestination>> CreateProjectionExpression<TDestination>()
        {
            var tuple = new TypeTuple(typeof(TSource), typeof(TDestination));
            return (Expression<Func<TSource, TDestination>>) Config.CreateMapExpression(tuple, MapType.Projection);
        }
    }
}
