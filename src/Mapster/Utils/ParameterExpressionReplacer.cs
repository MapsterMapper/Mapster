using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace Mapster.Utils
{
    sealed class ParameterExpressionReplacer : ExpressionVisitor
    {
        //fields
        readonly ReadOnlyCollection<ParameterExpression> _from;
        readonly Expression[] _to;

        public int[] ReplaceCounts { get; }

        //constructors
        public ParameterExpressionReplacer(ReadOnlyCollection<ParameterExpression> from, params Expression[] to)
        {
            _from = from;
            _to = to;
            ReplaceCounts = new int[_to.Length];
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            for (var i = 0; i < _from.Count; i++)
            {
                if (node != _from[i])
                    continue;
                if (i >= _to.Length)
                    return node.Type.CreateDefault();

                ReplaceCounts[i]++;
                return _to[i].To(node.Type, true);
            }
            return node;
        }

        protected override Expression VisitConditional(ConditionalExpression node)
        {
            var cond = (ConditionalExpression)base.VisitConditional(node);
            if (cond.Test.NodeType != ExpressionType.Equal && cond.Test.NodeType != ExpressionType.NotEqual)
                return cond;
            var bin = (BinaryExpression)cond.Test;
            var leftIsNull = bin.Left.NodeType == ExpressionType.Constant &&
                ((ConstantExpression)bin.Left).Value == null;
            var rightIsNull = bin.Right.NodeType == ExpressionType.Constant &&
                ((ConstantExpression)bin.Right).Value == null;
            if (!leftIsNull && !rightIsNull)
                return cond;
            var equal = cond.Test.NodeType == ExpressionType.Equal;
            var target = leftIsNull ? bin.Right : bin.Left;
            if (target!.CanBeNull())
                return cond;
            return equal ? cond.IfFalse : cond.IfTrue;
        }
    }
}
