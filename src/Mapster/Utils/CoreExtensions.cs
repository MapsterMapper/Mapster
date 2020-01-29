using System.Collections.Generic;

namespace Mapster.Utils
{
    internal static class CoreExtensions
    {
        public static string ToPascalCase(this string name)
        {
            return NameMatchingStrategy.PascalCase(name);
        }

#if !NETCOREAPP2_0
        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source)
        {
            return new HashSet<T>(source);
        }

        public static U GetValueOrDefault<T, U>(this IDictionary<T, U> dict, T key)
        {
            return dict.TryGetValue(key, out var value) ? value : default;
        }
#endif

    }
}
