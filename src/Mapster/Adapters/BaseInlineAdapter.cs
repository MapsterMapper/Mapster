using System;
using System.Linq.Expressions;

namespace Mapster.Adapters
{
    public abstract class BaseInlineAdapter
    {
        public abstract int? Priority(Type sourceType, Type desinationType, MapType mapType);

        public virtual LambdaExpression CreateAdaptFunc(CompileArgument arg)
        {
            //var depth = Expression.Parameter(typeof(int));
            var p = Expression.Parameter(arg.SourceType);
            var body = CreateExpressionBody(p, null, arg);
            return Expression.Lambda(body, p);
        }

        public virtual LambdaExpression CreateAdaptToTargetFunc(CompileArgument arg)
        {
            //var depth = Expression.Parameter(typeof(int));
            var p = Expression.Parameter(arg.SourceType);
            var p2 = Expression.Parameter(arg.DestinationType);
            var body = CreateExpressionBody(p, p2, arg);
            return Expression.Lambda(body, p, p2);
        }

        public abstract Expression CreateExpression(Expression source, Expression destination, Type destinationType);
    }
}
