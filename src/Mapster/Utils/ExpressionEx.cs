using System.Linq.Expressions;
using Mapster.Models;

namespace Mapster.Utils
{
    internal static class ExpressionEx
    {
        public static Expression Assign(Expression left, Expression right)
        {
            var middle = left.Type == right.Type
                ? right
                : Expression.Convert(right, left.Type);
            return Expression.Assign(left, middle);
        }

        public static Expression Apply(this LambdaExpression lambda, Expression exp)
        {
            var replacer = new ParameterExpressionReplacer(lambda.Parameters[0], exp);
            return replacer.Visit(lambda.Body);
        }
    }
}
