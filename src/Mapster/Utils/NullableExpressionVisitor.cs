using System.Linq.Expressions;

namespace Mapster.Utils
{
    sealed class NullableExpressionVisitor : ExpressionVisitor
    {
        public bool? CanBeNull { get; private set; }

        public override Expression Visit(Expression node)
        {
            if (CanBeNull.HasValue || node == null)
                return node;
            switch (node.NodeType)
            {
                case ExpressionType.AndAlso:
                case ExpressionType.ArrayLength:
                case ExpressionType.Equal:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.IsFalse:
                case ExpressionType.IsTrue:
                case ExpressionType.Lambda:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.ListInit:
                case ExpressionType.MemberInit:
                case ExpressionType.New:
                case ExpressionType.NewArrayBounds:
                case ExpressionType.NewArrayInit:
                case ExpressionType.NotEqual:
                case ExpressionType.OrElse:
                case ExpressionType.Quote:
                case ExpressionType.Throw:
                case ExpressionType.TypeEqual:
                case ExpressionType.TypeIs:
                case ExpressionType.Unbox:
                    CanBeNull = false;
                    return node;
                case ExpressionType.Assign:
                case ExpressionType.Coalesce:
                case ExpressionType.Conditional:
                case ExpressionType.Constant:
                case ExpressionType.Convert:
                case ExpressionType.ConvertChecked:
                    return base.Visit(node);
                default:
                    CanBeNull = true;
                    return node;
            }
        }

        void VisitBoth(Expression left, Expression right)
        {
            CanBeNull = null;
            Visit(left);
            var leftCanNull = CanBeNull;

            CanBeNull = null;
            Visit(right);
            var rightCanNull = CanBeNull;

            CanBeNull = leftCanNull.GetValueOrDefault() || rightCanNull.GetValueOrDefault();
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            if (node.NodeType == ExpressionType.Assign || node.NodeType == ExpressionType.Coalesce)
                Visit(node.Right);
            else
                VisitBoth(node.Left, node.Right);
            return node;
        }

        protected override Expression VisitUnary(UnaryExpression node)
        {
            if (node.NodeType == ExpressionType.Convert || node.NodeType == ExpressionType.ConvertChecked)
                Visit(node.Operand);
            return node;
        }

        protected override Expression VisitConditional(ConditionalExpression node)
        {
            VisitBoth(node.IfTrue, node.IfFalse);
            return node;
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            CanBeNull = node.Value == null;
            return node;
        }
    }
}
