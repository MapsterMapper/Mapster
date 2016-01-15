using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace Mapster.Models
{
    sealed class ParameterExpressionReplacer : ExpressionVisitor
    {
        //fields
        readonly ReadOnlyCollection<ParameterExpression> _from;
        readonly Expression[] _to;

        //constructors
        public ParameterExpressionReplacer(ReadOnlyCollection<ParameterExpression> from, Expression[] to)
        {
            _from = from;
            _to = to;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            for (var i = 0; i < _from.Count; i++)
            {
                if (node == _from[i])
                {
                    if (node.Type == _to[i].Type)
                        return _to[i];
                    else
                        return Expression.Convert(_to[i], node.Type);
                }
            }
            return node;
        }
    }
}
