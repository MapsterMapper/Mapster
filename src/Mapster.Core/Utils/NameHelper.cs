using System.Collections.Generic;
using System.Linq;

namespace Mapster.Utils
{
    static class NameHelper
    {
        internal static string Identity(string s) => s;
        internal static string PascalCase(string s) => string.Concat(BreakWords(s).Select(ProperCase));
        internal static string CamelCase(string s) => string.Concat(BreakWords(s).Select((w, i) => i == 0 ? w.ToLower() : ProperCase(w)));
        internal static string LowerCase(string s) => s.ToLower();

        private static string ProperCase(string s) => s.Length == 0 ? s : (char.ToUpper(s[0]) + s.Substring(1).ToLower());

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
