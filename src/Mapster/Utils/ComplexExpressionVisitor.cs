using System.Linq.Expressions;

namespace Mapster.Utils
{
    sealed class ComplexExpressionVisitor : ExpressionVisitor
    {
        public bool IsComplex { get; private set; }
        public override Expression Visit(Expression node)
        {
            if (node == null)
                return node;
            switch (node.NodeType)
            {
                case ExpressionType.ArrayIndex:
                case ExpressionType.ArrayLength:
                case ExpressionType.Constant:
                case ExpressionType.Convert:
                case ExpressionType.ConvertChecked:
                case ExpressionType.Default:
                case ExpressionType.Index:
                case ExpressionType.IsFalse:
                case ExpressionType.IsTrue:
                case ExpressionType.MemberAccess:
                case ExpressionType.Negate:
                case ExpressionType.NegateChecked:
                case ExpressionType.Not:
                case ExpressionType.OnesComplement:
                case ExpressionType.Parameter:
                case ExpressionType.UnaryPlus:
                case ExpressionType.Unbox:
                    return base.Visit(node);
                default:
                    IsComplex = true;
                    return node;
            }
        }
    }
}
