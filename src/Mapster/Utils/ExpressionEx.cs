using System.Linq.Expressions;
using Mapster.Models;
using System;

namespace Mapster.Utils
{
    internal static class ExpressionEx
    {
        public static Expression Assign(Expression left, Expression right)
        {
            var middle = left.Type.IsReferenceAssignableFrom(right.Type)
                ? right
                : Expression.Convert(right, left.Type);
            return Expression.Assign(left, middle);
        }

        public static Expression Apply(this LambdaExpression lambda, params Expression[] exps)
        {
            var replacer = new ParameterExpressionReplacer(lambda.Parameters, exps);
            return replacer.Visit(lambda.Body);
        }

        public static Expression TrimConversion(this Expression exp)
        {
            while (exp.NodeType == ExpressionType.Convert || exp.NodeType == ExpressionType.ConvertChecked)
            {
                exp = ((UnaryExpression)exp).Operand;
            }
            return exp;
        }

        public static Expression To(this Expression exp, Type type, bool force = false)
        {
            bool sameType = force ? type == exp.Type : type.IsReferenceAssignableFrom(exp.Type);
            if (sameType)
                return exp;
            else
                return Expression.Convert(exp, type);
        }
    }
}
