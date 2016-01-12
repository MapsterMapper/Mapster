using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

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
    public static class MapContext
    {
        [ThreadStatic]
        private static Dictionary<object, object> _references;
        public static Dictionary<object, object> References
        {
            get { return _references ?? (_references = new Dictionary<object, object>(ReferenceComparer.Default)); }
        }

        public static void Clear()
        {
            _references = null;
        }
    }
}
