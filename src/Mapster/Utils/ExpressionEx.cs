using System.Linq.Expressions;

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

    }
}
