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
    }
}
