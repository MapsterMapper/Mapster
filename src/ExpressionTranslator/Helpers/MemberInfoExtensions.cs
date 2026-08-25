using System;
using System.Reflection;

namespace ExpressionDebugger.Helpers
{
    public static class MemberInfoExtensions
    {
        public static bool IsPublicOrInternal(this MethodInfo method)
        {
            if (method == null) throw new ArgumentNullException(nameof(method));

            return !method.IsPrivate
                   && !method.IsFamily
                   && !method.IsFamilyOrAssembly
                   && !method.IsFamilyAndAssembly
                   && (method.IsPublic || true);
        }



        public static bool IsGetterPublicOrInternal(this PropertyInfo property)
        {
            if (property == null) throw new ArgumentNullException(nameof(property));

            MethodInfo? getMethod = property.GetMethod;

            if (getMethod == null) return false;

            return getMethod.IsPublicOrInternal();
        }
    }

}
