using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Mapster.Utils
{
    public static class MapsterHelper
    {
        public static U GetValueOrDefault<T, U>(IDictionary<T, U> dict, T key)
        {
            return dict.TryGetValue(key, out var value) ? value : default!;
        }

        public static U FlexibleGet<U>(IDictionary<string, U> dict, string key, Func<string, string> keyConverter)
        {
            return (from kvp in dict
                    where keyConverter(kvp.Key) == key
                    select kvp.Value).FirstOrDefault();
        }

        public static void FlexibleSet<U>(IDictionary<string, U> dict, string key, Func<string, string> keyConverter, U value)
        {
            var dictKey = (from kvp in dict
                           where keyConverter(kvp.Key) == key
                           select kvp.Key).FirstOrDefault();
            dict[dictKey ?? key] = value;
        }

        public static IEnumerable<T> ToEnumerable<T>(IEnumerable<T> source)
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

        public static readonly Func<string, string> Identity = NameMatchingStrategy.Identity;
        public static readonly Func<string, string> PascalCase = NameMatchingStrategy.PascalCase;
        public static readonly Func<string, string> CamelCase = NameMatchingStrategy.CamelCase;
        public static readonly Func<string, string> LowerCase = NameMatchingStrategy.LowerCase;

        internal static Expression GetConverterExpression(Func<string, string> converter)
        {
            if (converter == NameMatchingStrategy.Identity)
                return Expression.Field(null, typeof(MapsterHelper), nameof(Identity));
            if (converter == NameMatchingStrategy.PascalCase)
                return Expression.Field(null, typeof(MapsterHelper), nameof(PascalCase));
            if (converter == NameMatchingStrategy.CamelCase)
                return Expression.Field(null, typeof(MapsterHelper), nameof(CamelCase));
            if (converter == NameMatchingStrategy.LowerCase)
                return Expression.Field(null, typeof(MapsterHelper), nameof(LowerCase));
            return Expression.Constant(converter);
        }
    }
}
