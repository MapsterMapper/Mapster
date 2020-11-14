using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Mapster.Utils
{
    sealed class NullableExpressionVisitor : ExpressionVisitor
    {
        public bool? CanBeNull { get; private set; }

        public override Expression? Visit(Expression? node)
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
                case ExpressionType.Call:
                case ExpressionType.Coalesce:
                case ExpressionType.Conditional:
                case ExpressionType.Constant:
                case ExpressionType.Convert:
                case ExpressionType.ConvertChecked:
                case ExpressionType.MemberAccess:
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

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            CanBeNull = node.Method.ReturnParameter?.GetCustomAttributesData().All(IsNullable) ?? true;
            return node;
        }

        private static bool IsNullable(CustomAttributeData attr)
        {
            if (attr.GetAttributeType().Name != "NullableAttribute")
                return true;

            return IsNullable(attr.ConstructorArguments);
        }

        private static bool IsNullable(IList<CustomAttributeTypedArgument> args)
        {
            if (args.Count == 0)
                return true;

            var arg = args[0];
            if (arg.Value is byte[] bytes)
                return bytes.Length == 0 || bytes[0] != 1;
            else if (arg.Value is IList<CustomAttributeTypedArgument> a)
                return IsNullable(a);
            else if (arg.Value is byte b)
                return b != 1;
            else
                return true;
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

        protected override Expression VisitMember(MemberExpression node)
        {
            CanBeNull = node.Member.GetCustomAttributesData().All(IsNullable);
            return node;
        }
    }
}
