using System.Collections.Generic;

namespace Mapster
{
    public class TypeAdapterBuiler<TSource>
    {
        TSource Source { get; }
        TypeAdapterConfig Config { get; }

        private Dictionary<string, object> _parameters;
        Dictionary<string, object> Parameters
        {
            get { return _parameters ?? (_parameters = new Dictionary<string, object>(ReferenceComparer.Default)); }
        }

        internal TypeAdapterBuiler(TSource source, TypeAdapterConfig config)
        {
            this.Source = source;
            this.Config = config;
        }

        public TypeAdapterBuiler<TSource> AddParameters(string name, object value)
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
    }
}