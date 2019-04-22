using Mapster.Utils;
using System;
using System.Linq.Expressions;

namespace Mapster.Adapters
{
    internal class DelegateAdapter : BaseAdapter
    {
        readonly LambdaExpression _lambda;
        public DelegateAdapter(LambdaExpression lambda)
        {
            _lambda = lambda;
        }

        protected override bool CanMap(PreCompileArgument arg)
        {
            throw new NotImplementedException();
        }

        protected override Expression CreateInstantiationExpression(Expression source, Expression? destination, CompileArgument arg)
        {
            if (destination == null)
                return _lambda.Apply(arg.MapType, source);
            else
                return _lambda.Apply(arg.MapType, source, destination);
        }

        protected override Expression CreateBlockExpression(Expression source, Expression destination, CompileArgument arg)
        {
            return Expression.Empty();
        }

        protected override Expression CreateInlineExpression(Expression source, CompileArgument arg)
        {
            return CreateInstantiationExpression(source, arg);
        }
    }
}
