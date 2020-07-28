using System.Linq.Expressions;

namespace Mapster.Adapters
{
    internal class ObjectAdapter : BaseAdapter
    {
        protected override int Score => -111;   //must do before all class adapters

        protected override bool CanMap(PreCompileArgument arg)
        {
            return arg.SourceType == typeof(object) || arg.DestinationType == typeof(object);
        }

        protected override Expression CreateInstantiationExpression(Expression source, Expression? destination, CompileArgument arg)
        {
            var srcType = arg.SourceType;
            var destType = arg.DestinationType;
            if (srcType == destType)
                return source;
            if (destType == typeof(object))
                return Expression.Convert(source, destType);
            return arg.Context.Config.CreateDynamicMapInvokeExpressionBody(arg.DestinationType, source);
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
