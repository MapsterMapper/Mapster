using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Mapster.Diagnostics
{
    internal static class Extensions
    {
#if NET40
        public static Type GetTypeInfo(this Type type) {
            return type;
        }
#endif

        public static Expression Assign(Expression left, Expression right)
        {
            var middle = left.Type.IsReferenceAssignableFrom(right.Type)
                ? right
                : Expression.Convert(right, left.Type);
            return Expression.Assign(left, middle);
        }

        public static Expression TrimConversion(this Expression exp, bool force = false)
        {
            while (exp.NodeType == ExpressionType.Convert || exp.NodeType == ExpressionType.ConvertChecked)
            {
                var unary = (UnaryExpression)exp;
                if (force || unary.Type.IsReferenceAssignableFrom(unary.Operand.Type))
                    exp = unary.Operand;
                else
                    break;
            }
            return exp;
        }

        public static Expression To(this Expression exp, Type type, bool force = false)
        {
            exp = exp.TrimConversion();
            bool sameType = force ? type == exp.Type : type.IsReferenceAssignableFrom(exp.Type);
            if (sameType)
                return exp;
            else
                return Expression.Convert(exp, type);
        }

        public static bool IsReferenceAssignableFrom(this Type destType, Type srcType)
        {
            if (destType == srcType)
                return true;

            if (!destType.GetTypeInfo().IsValueType && !srcType.GetTypeInfo().IsValueType && destType.GetTypeInfo().IsAssignableFrom(srcType.GetTypeInfo()))
                return true;

            return false;
        }

    }
}
