using System.Collections.Generic;

namespace Mapster.Utils
{
    internal static class Extensions
    {
        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source)
        {
            return new HashSet<T>(source);
        }

        public static string ToPascalCase(this string name)
        {
            return NameMatchingStrategy.ToPascalCase(name);
        }

        public static U GetValueOrDefault<T, U>(this IDictionary<T, U> dict, T key)
        {
            U value;
            return dict.TryGetValue(key, out value) ? value : default(U);
        }
    }
}
