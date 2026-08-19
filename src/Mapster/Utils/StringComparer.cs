using System;
using System.Collections.Generic;

namespace Mapster.Utils
{
    internal class MapsterStringComparer : IEqualityComparer<string>
    {
        public bool Equals(string? x, string? y)
        {
            if(String.IsNullOrEmpty(x) || String.IsNullOrEmpty(y))
                return false;

           return String.Equals(x, y, StringComparison.InvariantCultureIgnoreCase);
        }

        public int GetHashCode(string obj)
        {
            if(obj is null)
                return 0;
            
            return obj.GetHashCode();
        }
    }
}
