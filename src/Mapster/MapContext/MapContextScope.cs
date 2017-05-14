using System;

namespace Mapster
{
    public class MapContextScope : IDisposable
    {
        public MapContext Context { get; }

        private readonly bool _isRootScope;
        public MapContextScope()
        {
            this.Context = MapContext.Current;
            if (this.Context == null)
            {
                _isRootScope = true;
                this.Context = MapContext.Current = new MapContext();
            }
        }

        public void Dispose()
        {
            if (_isRootScope && ReferenceEquals(MapContext.Current, this.Context))
                MapContext.Current = null;
        }

        public static TResult GetOrAddMapReference<TKey, TResult>(TKey key, Func<TKey, TResult> mapFn)
        {
            using (var context = new MapContextScope())
            {
                var dict = context.Context.References;
                if (!dict.TryGetValue(key, out var reference))
                    dict[key] = reference = mapFn(key);
                return (TResult)reference;
            }
        }
    }
}
