using System;
using System.Collections.Generic;
using System.Linq;

namespace Mapster
{
    public class NameMatchingStrategy
    {
        public Func<string, string> SourceMemberNameConverter { get; set; }
        public Func<string, string> DestinationMemberNameConverter { get; set; }

        public void Apply(NameMatchingStrategy other)
        {
            if (this.SourceMemberNameConverter == null)
                this.SourceMemberNameConverter = other.SourceMemberNameConverter;
            if (this.DestinationMemberNameConverter == null)
                this.DestinationMemberNameConverter = other.DestinationMemberNameConverter;
        }

        public static readonly NameMatchingStrategy Exact = new NameMatchingStrategy
        {
            SourceMemberNameConverter = Identity,
            DestinationMemberNameConverter = Identity,
        };

        public static readonly NameMatchingStrategy Flexible = new NameMatchingStrategy
        {
            SourceMemberNameConverter = ToPascalCase,
            DestinationMemberNameConverter = ToPascalCase,
        };

        private static string Identity(string s) => s;

        internal static string ToPascalCase(string s) => string.Join("", BreakWords(s).Select(ToProperCase));

        private static string ToProperCase(string s) => s.Length == 0 ? s : (char.ToUpper(s[0]) + s.Substring(1).ToLower());

        enum WordType
        {
            Unknown,
            UpperCase,
            LowerCase,
            ProperCase,
        }
        private static IEnumerable<string> BreakWords(string s)
        {
            var len = s.Length;
            var pos = 0;
            var last = 0;
            var type = WordType.Unknown;

            while (pos < len)
            {
                var c = s[pos];
                if (c == '_')
                {
                    if (last < pos)
                    {
                        yield return s.Substring(last, pos - last);
                        last = pos;
                        type = WordType.Unknown;
                    }
                    last++;
                }
                else if (char.IsUpper(c))
                {
                    if (type == WordType.Unknown)
                        type = WordType.UpperCase;
                    else if (type != WordType.UpperCase && last < pos)
                    {
                        yield return s.Substring(last, pos - last);
                        last = pos;
                        type = WordType.UpperCase;
                    }
                }
                else  //lower
                {
                    if (type == WordType.Unknown)
                        type = WordType.LowerCase;
                    else if (type == WordType.UpperCase)
                    {
                        if (last < pos - 1)
                        {
                            yield return s.Substring(last, pos - last - 1);
                            last = pos - 1;
                        }
                        type = WordType.ProperCase;
                    }
                }
                pos++;
            }
            if (last < pos)
                yield return s.Substring(last, pos - last);
        }
    }
}
