using System.Collections.Generic;

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

        public static U GetValueOrDefault<T, U>(this IDictionary<T, U> dict, T key)
        {
            U value;
            return dict.TryGetValue(key, out value) ? value : default(U);
        }
    }
}
