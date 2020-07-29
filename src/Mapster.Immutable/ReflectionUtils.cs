using System;
using System.Collections;
using System.Reflection;

namespace Mapster.Immutable
{
    public static class ReflectionUtils
    {
        public static bool IsCollection(this Type type)
        {
            return typeof(IEnumerable).GetTypeInfo().IsAssignableFrom(type.GetTypeInfo()) && type != typeof(string);
        }

    }
}
