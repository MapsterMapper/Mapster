using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
// ReSharper disable StaticMemberInGenericType
#pragma warning disable 8618

namespace Mapster.Utils
{
    public static class Enum<TEnum> where TEnum : struct
    {
        private static readonly Dictionary<string, TEnum> _lookup = ((TEnum[])Enum.GetValues(typeof(TEnum))).ToDictionary(x => x.ToString().ToUpperInvariant(), y => y);
        private static readonly Dictionary<TEnum, string> _reverseLookup = ((TEnum[])Enum.GetValues(typeof(TEnum))).ToDictionary(x => x, y => y.ToString());
        private static readonly bool _isFlag = typeof(TEnum).GetTypeInfo().IsDefined(typeof(FlagsAttribute), true);
        private static readonly Type _underlyingType = Enum.GetUnderlyingType(typeof (TEnum));
        private static readonly Func<TEnum, ulong> _toUlong;
        private static readonly Func<ulong, TEnum> _toEnum;
        private static readonly Func<TEnum, string> _toIntString;
        private static readonly Func<string, TEnum> _toIntEnum;

        private static readonly ulong[] _flagList;
        private static readonly string _zeroString;

        static Enum()
        {
            var p = Expression.Parameter(typeof (TEnum));
            _toUlong = Expression.Lambda<Func<TEnum, ulong>>(Expression.Convert(p, typeof (ulong)), p).Compile();
            var p2 = Expression.Parameter(typeof (ulong));
            _toEnum = Expression.Lambda<Func<ulong, TEnum>>(Expression.Convert(p2, typeof(TEnum)), p2).Compile();

            var toString = _underlyingType.GetMethod("ToString", Type.EmptyTypes);
            var p3 = Expression.Parameter(typeof (TEnum));
            _toIntString = Expression.Lambda<Func<TEnum, string>>(
                Expression.Call(Expression.Convert(p3, _underlyingType), toString),
                p3).Compile();
            var parse = _underlyingType.GetMethod("Parse", new[] {typeof (string)});
            var p4 = Expression.Parameter(typeof(string));
            _toIntEnum = Expression.Lambda<Func<string, TEnum>>(
                Expression.Convert(Expression.Call(parse, p4), typeof (TEnum)),
                p4).Compile();

            if (_isFlag)
            {
                _flagList = ((TEnum[]) Enum.GetValues(typeof (TEnum))).Select(_toUlong).Where(x => x > 0).OrderBy(x => x).ToArray();
                _zeroString = _toEnum(0).ToString();
            }
        }

        /// <summary>
        /// Converts enum to string using dictionary to avoid reflection perf hit.
        /// </summary>
        /// <param name="val">Enum to evaluate</param>
        /// <returns></returns>
        public static string ToString(TEnum val)
        {
            if (!_isFlag)
            {
                if (_reverseLookup.TryGetValue(val, out var ret))
                    return ret;
                return _toIntString(val);
            }

            var i = _toUlong(val);
            var pos = Array.BinarySearch(_flagList, i);
            if (pos < 0)
                pos = ~pos - 1;

            var list = new List<ulong>();
            for (var j = pos; j >= 0 && i > 0; j--)
            {
                var k = _flagList[j];
                if ((k & i) == k)
                {
                    list.Add(k);
                    i ^= k;
                }
            }
            if (i == 0)
            {
                if (list.Count == 0)
                    return _zeroString;
                list.Reverse();
                return string.Join(", ", list.Select(x => _reverseLookup[_toEnum(x)]));
            }
            return _toIntString(val);
        }

        /// <summary>
        /// Converts string to Enum using case insensitive comparison
        /// </summary>
        /// <param name="val">String to evaluate</param>
        /// <returns></returns>
        public static TEnum Parse(string val)
        {
            if (string.IsNullOrEmpty(val))
                return _toEnum(0);

            var ret = val.Split(',')
                .Select(MatchByString)
                .Aggregate(0ul, (a, b) => a | _toUlong(b));
            return _toEnum(ret);
        }

        internal static string ValidChoices()
        {
            return string.Join(",", _reverseLookup.Values);
        }

        private static TEnum MatchByString(string val)
        {
            if (!_lookup.TryGetValue(val.Trim().ToUpperInvariant(), out var match))
                return _toIntEnum(val);

            return match;
        }
    }
}
