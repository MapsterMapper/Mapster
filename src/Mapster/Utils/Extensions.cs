using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapster.Utils
{
    internal static class Extensions
    {
        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source)
        {
            return new HashSet<T>(source);
        }

        public static string ToProperCase(this string name)
        {
            return name.Substring(0, 1).ToUpper() + name.Substring(1);
        }
    }
}
