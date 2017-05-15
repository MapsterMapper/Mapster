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

        protected override bool CanMap(Type sourceType, Type destinationType, MapType mapType)
        {
            throw new NotImplementedException();
        }

        protected override Expression CreateInstantiationExpression(Expression source, Expression destination, CompileArgument arg)
        {
            return _lambda.Apply(source, destination);
        }

        protected override Expression CreateBlockExpression(Expression source, Expression destination, CompileArgument arg)
        {
            return Expression.Empty();
        }

        protected override Expression CreateInlineExpression(Expression source, CompileArgument arg)
        {
            return Expression.Empty();
        }
    }
}
