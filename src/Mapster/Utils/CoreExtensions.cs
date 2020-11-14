using System.Collections.Generic;

namespace Mapster.Utils
{
    internal static class CoreExtensions
    {
        public static string ToPascalCase(this string name)
        {
            return MapsterHelper.PascalCase(name);
        }

        public static void LockAdd<T>(this List<T> list, T item)
        {
            lock (list)
            {
                list.Add(item);
            }
        }

        public static void LockRemove<T>(this List<T> list, T item)
        {
            lock (list)
            {
                list.Remove(item);
            }
        }

        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source)
        {
            return new HashSet<T>(source);
        }
    }
}
