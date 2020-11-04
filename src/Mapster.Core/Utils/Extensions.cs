using System;

namespace Mapster.Utils
{
    static class Extensions
    {
#if NET40
        public static Type GetTypeInfo(this Type type)
        {
            return type;
        }

#endif
    }
}
