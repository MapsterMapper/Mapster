using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Mapster
{
    internal class ReferenceComparer : IEqualityComparer<object>
    {
        public static readonly ReferenceComparer Default = new ReferenceComparer();

        bool IEqualityComparer<object>.Equals(object x, object y)
        {
            return ReferenceEquals(x, y);
        }

        public int GetHashCode(object obj)
        {
            return RuntimeHelpers.GetHashCode(obj);
        }
    }

    /// <summary>
    /// This class is to send data between mapping process
    /// </summary>
    /// <remarks>
    /// The idea of this class is similar to Transaction & TransactionScope
    /// You can get context by MapContext.Current
    /// And all mapping processes will having only one context
    /// </remarks>
    internal class MapContext
    {
        [ThreadStatic]
        private static MapContext _current;
        public static MapContext Current
        {
            get { return _current; }
            set { _current = value; }
        }

        private Dictionary<object, object> _references;
        public Dictionary<object, object> References
        {
            get { return _references ?? (_references = new Dictionary<object, object>(ReferenceComparer.Default)); }
        }
    }
    internal class MapContextScope : IDisposable
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
