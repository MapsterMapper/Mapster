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
    public class MapContext
    {
        public Dictionary<object, object> References { get; private set; }

        internal static MapContext Create()
        {
            return new MapContext
            {
                References = TypeAdapterConfig.GlobalSettings.PreserveReference
                    ? new Dictionary<object, object>(ReferenceComparer.Default)
                    : null
            };
        }
    }
}
