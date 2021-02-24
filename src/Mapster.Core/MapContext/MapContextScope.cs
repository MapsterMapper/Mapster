using System;
using Mapster.Utils;

namespace Mapster
{
    public class MapContextScope : IDisposable
    {
        public MapContext Context { get; }

        private readonly MapContext? _oldContext;

        public MapContextScope()
        {
            _oldContext = MapContext.Current;

            this.Context = new MapContext();
            if (_oldContext != null)
            {
                foreach (var parameter in _oldContext.Parameters)
                {
                    this.Context.Parameters[parameter.Key] = parameter.Value;
                }
                foreach (var reference in _oldContext.References)
                {
                    this.Context.References[reference.Key] = reference.Value;
                }
            }

            MapContext.Current = this.Context;
        }

        public void Dispose()
        {
            MapContext.Current = _oldContext;
        }

        public static TResult GetOrAddMapReference<TResult>(ReferenceTuple key, Func<ReferenceTuple, TResult> mapFn) where TResult : notnull
        {
            using var context = new MapContextScope();
            var dict = context.Context.References;
            if (!dict.TryGetValue(key, out var reference))
                dict[key] = reference = mapFn(key);
            return (TResult)reference;
        }
    }
}
