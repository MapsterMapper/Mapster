using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Mapster.Utils
{
    sealed class NewExpressionVisitor : ExpressionVisitor
    {
        public bool? IsNew { get; private set; }

        public override Expression Visit(Expression node)
        {
            if (IsNew.HasValue || node == null)
                return node;
            
            switch (node.NodeType)
            {
                case ExpressionType.Assign:
                case ExpressionType.Coalesce:
                case ExpressionType.Conditional:
                case ExpressionType.Convert:
                case ExpressionType.ConvertChecked:
                case ExpressionType.Constant:
                case ExpressionType.Unbox:
                case ExpressionType.TypeAs:
                    return base.Visit(node);

                case ExpressionType.Default:
                case ExpressionType.Lambda:
                case ExpressionType.ListInit:
                case ExpressionType.MemberInit:
                case ExpressionType.New:
                case ExpressionType.NewArrayBounds:
                case ExpressionType.NewArrayInit:
                case ExpressionType.Throw:
                    IsNew = true;
                    return node;
            }

            if (node is UnaryExpression || node is BinaryExpression || node is TypeBinaryExpression)
            {
                IsNew = true;
                return node;
            }

            IsNew = false;
            return node;
        }

        void VisitBoth(Expression left, Expression right)
        {
            IsNew = null;
            Visit(left);
            var leftIsNew = IsNew;

            IsNew = null;
            Visit(right);
            var rightIsNew = IsNew;

            IsNew = leftIsNew.GetValueOrDefault() && rightIsNew.GetValueOrDefault();
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            if (node.NodeType == ExpressionType.Assign)
                Visit(node.Right);
            else
                VisitBoth(node.Left, node.Right);
            return node;
        }

        protected override Expression VisitConditional(ConditionalExpression node)
        {
            VisitBoth(node.IfTrue, node.IfFalse);
            return node;
        }

        protected override Expression VisitUnary(UnaryExpression node)
        {
            Visit(node.Operand);
            return node;
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            if (node.Value == null)
                IsNew = true;
            return node;
        }
    }
}
