using System;
using System.Linq.Expressions;

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

    }
}
