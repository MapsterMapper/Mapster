using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Mapster
{
    internal class ReferenceComparer : IEqualityComparer<object>
    {
        public static readonly ReferenceComparer Default = new ReferenceComparer();

        public bool Equals(object x, object y)
        {
            return object.ReferenceEquals(x, y);
        }

        public int GetHashCode(object obj)
        {
            return RuntimeHelpers.GetHashCode(obj);
        }
    }
    public class MapContext
    {
        [ThreadStatic]
        private static MapContext _context;
        public static MapContext Context
        {
            get { return _context ?? (_context = new MapContext()); }
        }

        private Dictionary<object, object> _references;
        public Dictionary<object, object> References
        {
            get { return _references ?? (_references = new Dictionary<object, object>(ReferenceComparer.Default)); }
        }

        public static void Clear()
        {
            _context = null;
        }
    }
}
