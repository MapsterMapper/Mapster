using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace Mapster.Utils
{
    sealed class ParameterExpressionReplacer : ExpressionVisitor
    {
        //fields
        readonly ReadOnlyCollection<ParameterExpression> _from;
        readonly Expression[] _to;

        public int ReplaceCount { get; private set; }

        //constructors
        public ParameterExpressionReplacer(ReadOnlyCollection<ParameterExpression> from, params Expression[] to)
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
                    this.ReplaceCount += 1;
                    if (i >= _to.Length)
                        return Expression.Constant(node.Type.GetDefault(), node.Type);
                    else if (node.Type == _to[i].Type)
                        return _to[i];
                    else
                        return Expression.Convert(_to[i], node.Type);
                }
            }
            return node;
        }
    }
}
