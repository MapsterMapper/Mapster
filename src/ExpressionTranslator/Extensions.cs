using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ExpressionDebugger
{
    internal static class Extensions
    {
        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source)
        {
            return new HashSet<T>(source);
        }

#if NET40
        public static Type GetTypeInfo(this Type type) {    
            return type;    
        }   
#endif

#if NET40 || NETSTANDARD1_3 || NET6_0_OR_GREATER
        public static T GetCustomAttribute<T>(this MemberInfo memberInfo) where T : Attribute
        {
            return (T)memberInfo.GetCustomAttributes(typeof(T), true).SingleOrDefault();
        }

        public static T GetCustomAttribute<T>(this Type type) where T : Attribute
        {
            return (T)type.GetTypeInfo().GetCustomAttributes(typeof(T), true).SingleOrDefault();
        }
#endif

        public static int FindStartIndex(this StringBuilder sb)
        {
            int wsCount = 0;
            for (int i = 0; i < sb.Length; i++)
            {
                if (char.IsWhiteSpace(sb[i]))
                    wsCount++;
                else
                    break;
            }
            return wsCount;
        }

        public static int FindEndIndex(this StringBuilder sb)
        {
            int wsCount = 0;
            for (int i = sb.Length - 1; i >= 0; i--)
            {
                if (char.IsWhiteSpace(sb[i]))
                    wsCount++;
                else
                    break;
            }
            return sb.Length - wsCount;
        }
    }
}
