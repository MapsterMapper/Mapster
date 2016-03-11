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

        private static string ToPascalCase(string s) => string.Join("", BreakWords(s).Select(ToProperCase));

        private static string ToProperCase(string s) => s.Length == 0 ? s : (char.ToUpper(s[0]) + s.Substring(1).ToLower());

        private static IEnumerable<string> BreakWords(string s)
        {
            var words = new List<string>();

            foreach (var word in s.Split('_'))
            {
                if (word.All(char.IsUpper))
                    words.Add(word);
                else
                    words.AddRange(SplitWord(word));
            }

            return words;
        }

        private static IEnumerable<string> SplitWord(string s)
        {
            var len = s.Length;
            var pos = 0;
            var last = 0;

            while (pos < len)
            {
                var c = s[pos];
                if (char.IsUpper(c))
                {
                    if (last < pos)
                    {
                        yield return s.Substring(last, pos - last);
                        last = pos;
                    }
                }
                pos++;
            }
            if (last < pos)
                yield return s.Substring(last, pos - last);
        }
    }
}
