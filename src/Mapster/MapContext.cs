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
    internal class MapContext
    {
        [ThreadStatic]
        private static MapContext _context;
        public static MapContext Context
        {
            get { return _context ?? (_context = new MapContext()); }
        }

        public static bool HasContext
        {
            get { return _context != null; }
        }

        private Dictionary<object, object> _references;
        public Dictionary<object, object> References
        {
            get { return _references ?? (_references = new Dictionary<object, object>(ReferenceComparer.Default)); }
        }

        public static void EnsureContext()
        {
            if (_context == null)
                _context = new MapContext();
        }

        public static void Clear()
        {
            _context = null;
        }
    }
}
