using System.Linq.Expressions;

namespace Mapster.Models
{
    sealed class ParameterExpressionReplacer : ExpressionVisitor
    {
        //fields
        readonly ParameterExpression _from;
        readonly Expression _to;

        //constructors
        public ParameterExpressionReplacer(ParameterExpression from, Expression to)
        {
            _from = from;
            _to = to;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            return node == _from ? _to : node;
        }
    }
}
