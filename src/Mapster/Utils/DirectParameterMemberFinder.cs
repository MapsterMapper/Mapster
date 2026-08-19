using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

public class DirectParameterMemberFinder : ExpressionVisitor
{
    private readonly bool _isCtrMapping;
    private readonly HashSet<Expression> _TargetParams;
    public List<Expression> FoundMembers { get; } = new();

    public DirectParameterMemberFinder(bool conctructorMapping = false, params Expression[] targetParams)
    {
        _TargetParams = new HashSet<Expression>(targetParams);
        _isCtrMapping = conctructorMapping;
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        if (_TargetParams.Contains(GetParametr(node)))
            if (_isCtrMapping)
                FoundMembers.Add(node);
            else
                FoundMembers.Add(node.Expression);

        return node;
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Object is MemberExpression mem && _TargetParams.Contains(GetParametr(mem)))
            FoundMembers.Add(mem);

        foreach (var arg in node.Arguments)
        {
            if (arg is MemberExpression member && _TargetParams.Contains(GetParametr(member)))
            {
                // if Method is static for Type && not Extention method
                if (node.Object == null && !node.Method.IsDefined(typeof(ExtensionAttribute), inherit: false))
                    FoundMembers.Add(member.Expression);
                else
                    FoundMembers.Add(member);
                continue;
            }

            if (arg.NodeType == ExpressionType.Call)
            {
                Visit(arg);
            }
        }

        return node;
    }

    protected override Expression VisitUnary(UnaryExpression node)
    {
        if (node.NodeType == ExpressionType.Convert || node.NodeType == ExpressionType.ConvertChecked)
        {
            var result = base.VisitUnary(node);
            return result;
        }
        return base.VisitUnary(node);
    }
       
    public IEnumerable<Expression> Find(Expression expression)
    {
        FoundMembers.Clear();
        Visit(expression);

        return FoundMembers;
    }
      

    private Expression GetParametr(MemberExpression member)
    {
        Expression current = member;

        while (current != null)
        {
            if (current is MemberExpression mem)
            {
                current = mem.Expression;
                continue;
            }

            if (current is ParameterExpression)
                return current;
            else
                current = new ReturnParametrVisitor().GetParam(current);
        }

        return Expression.Empty();
    }

    internal class ReturnParametrVisitor : ExpressionVisitor
    {
        private Expression parametr;

        protected override Expression VisitParameter(ParameterExpression node)
        {
            parametr = node;
            return node;
        }

        public Expression GetParam(Expression expression)
        {
            Visit(expression);
            return parametr;
        }
    }


}