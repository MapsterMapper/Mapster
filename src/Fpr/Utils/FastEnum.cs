using System;
using System.Collections.Generic;
using System.Linq;

namespace Fpr.Utils
{
    /// <summary>
    /// perf about 5 times faster than enums toString. 
    /// Inspired by this example, but made generic and used reverse lookup to cover unordered enums.
    /// http://craddock.it/c-enum-performance-trick/
    /// </summary>
    /// <typeparam name="TEnum"></typeparam>
    internal class FastEnum<TEnum>
    {
        private static readonly Dictionary<string, TEnum> _lookup = ((TEnum[])Enum.GetValues((typeof(TEnum)))).ToDictionary(x => x.ToString().ToLowerInvariant(), y => y);
        private static readonly Dictionary<TEnum, string> _reverseLookup = ((TEnum[])Enum.GetValues((typeof(TEnum)))).ToDictionary(x => x, y => y.ToString());

        public TEnum Value { get; set; }

        /// <summary>
        /// Creates instance from string value and matches to enum with case insensitive comparison.
        /// </summary>
        /// <param name="val">value to match</param>
        public FastEnum(string val)
        {
            Value = MatchByString(val);
        }

        /// <summary>
        /// Creates instance from enum value
        /// </summary>
        /// <param name="val">enum value</param>
        public FastEnum(TEnum val)
        {
            Value = val;
        }

        /// <summary>
        /// Converts enum to string using dictionary to avoid reflection perf hit.
        /// </summary>
        /// <param name="val">Enum to evaluate</param>
        /// <returns></returns>
        public static string ToString(TEnum val)
        {
            return _reverseLookup[val];
        }

        /// <summary>
        /// Converts string to Enum using case insensitive comparison
        /// </summary>
        /// <param name="val">String to evaluate</param>
        /// <returns></returns>
        public static TEnum ToEnum(string val)
        {
            return MatchByString(val);
        }

        public static string ValidChoices()
        {
            return string.Join(",", _reverseLookup.Values);
        }

        private static TEnum MatchByString(string val)
        {
            TEnum match;

            if (val == null)
            {
                return _lookup.First().Value;
            }
            
            if (val == string.Empty || !_lookup.TryGetValue(val.ToLowerInvariant(), out match))
            {
                throw new ArgumentOutOfRangeException(string.Format("'{0}' is invalid. Please choose from {1}", val, ValidChoices()));
            }
           
            return match;
        }

        public override string ToString()
        {
            return _reverseLookup[Value];
        }

    }
}