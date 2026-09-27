using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Mapster.Utils
{
    public static class Extensions
    {
#if NET40
        public static Type GetTypeInfo(this Type type)
        {
            return type;
        }
#endif

        public static string GetMemberName(this LambdaExpression lambda)
        {
            string? prop = null;
            var expr = lambda.Body;
            if (expr.NodeType == ExpressionType.MemberAccess)
            {
                var memEx = (MemberExpression)expr;
                prop = memEx.Member.Name;
                expr = (Expression?)memEx.Expression;
            }
            if (prop == null || expr?.NodeType != ExpressionType.Parameter)
                throw new ArgumentException("Allow only first level member access (eg. obj => obj.Name)", nameof(lambda));
            return prop;
        }

        public static IEnumerable<Type> GetLoadableTypes(this Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                return e.Types.Where(t => t != null).Cast<Type>();
            }
        }

        public static T? GetIfSingle<T>(this IEnumerable<T>? sequence)
        {
            if (sequence is null)
                return default;

            using var enumerator = sequence.GetEnumerator();

            if (!enumerator.MoveNext())
                return default;

            var firstElement = enumerator.Current;

            if (enumerator.MoveNext())
                return default;

            return firstElement;
        }
#if NETSTANDARD2_0

        public static bool TryAdd<TKey, TValue>(this Dictionary<TKey, TValue> source, TKey key, TValue value )
        {
            if(!source.ContainsKey(key))
            {
                source[key] = value;
                return true;
            }

            return false;
        }

#endif 
    }
}
