using System.Collections.Generic;

namespace Mapster.Utils
{
    internal static class CoreExtensions
    {
        public static string ToPascalCase(this string name)
        {
            return NameMatchingStrategy.PascalCase(name);
        }

#if !NETCOREAPP
        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source)
        {
            return new HashSet<T>(source);
        }
#endif

    }
}
