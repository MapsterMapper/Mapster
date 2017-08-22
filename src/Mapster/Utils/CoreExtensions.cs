using System;
using System.Collections.Generic;
using System.Linq;

namespace Mapster.Utils
{
    internal static class CoreExtensions
    {
        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source)
        {
            return new HashSet<T>(source);
        }

        public static string ToPascalCase(this string name)
        {
            return NameMatchingStrategy.PascalCase(name);
        }

        public static U GetValueOrDefault<T, U>(this IDictionary<T, U> dict, T key)
        {
            return dict.TryGetValue(key, out var value) ? value : default(U);
        }

        public static U FlexibleGet<U>(this IDictionary<string, U> dict, string key, Func<string, string> keyConverter)
        {
            return (from kvp in dict
                    where keyConverter(kvp.Key) == key
                    select kvp.Value).FirstOrDefault();
        }

        public static void FlexibleSet<U>(this IDictionary<string, U> dict, string key, Func<string, string> keyConverter, U value)
        {
            var dictKey = (from kvp in dict
                           where keyConverter(kvp.Key) == key
                           select kvp.Key).FirstOrDefault();
            dict[dictKey ?? key] = value;
        }

        public static IEnumerable<T> ToEnumerable<T>(this IEnumerable<T> source)
        {
            if (source is T[] array)
            {
                for (int i = 0, count = array.Length; i < count; i++)
                {
                    yield return array[i];
                }
            }
            else if (source is IList<T> list)
            {
                for (int i = 0, count = list.Count; i < count; i++)
                {
                    yield return list[i];
                }
            }
            else
            {
                foreach (var item in source)
                {
                    yield return item;
                }
            }
        }
    }
}
