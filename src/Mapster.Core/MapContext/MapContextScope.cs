using System;
using Mapster.Utils;

namespace Mapster
{
    public class MapContextScope : IDisposable
    {
        public static MapContextScope Required()
        {
            return new MapContextScope();
        }

        public static MapContextScope RequiresNew()
        {
            return new MapContextScope(true);
        }

        public MapContext Context { get; }

        private readonly MapContext? _previousContext;

        public MapContextScope() : this(false) { }
        public MapContextScope(bool ignorePreviousContext)
        {
            _previousContext = MapContext.Current;

            this.Context = ignorePreviousContext
                ? new MapContext()
                : _previousContext ?? new MapContext();

            MapContext.Current = this.Context;
        }

        public void Dispose()
        {
            MapContext.Current = _previousContext;
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
