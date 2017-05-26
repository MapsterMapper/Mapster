using System;
using System.Linq.Expressions;

namespace Mapster.Adapters
{
    internal class ObjectAdapter : BaseAdapter
    {
        protected override int Score => -111;

        protected override bool CanMap(PreCompileArgument arg)
        {
            return arg.SourceType == typeof(object) || arg.DestinationType == typeof(object);
        }

        protected override Expression CreateBlockExpression(Expression source, Expression destination, CompileArgument arg)
        {
            throw new NotImplementedException();
        }

        protected override Expression CreateInlineExpression(Expression source, CompileArgument arg)
        {
    //        object, T

    //if (src.GetType() == typeof(object))
    //            return (T)src

    //return src.Adapt<T>();

    //        poco, object

    //return (object)src.Adapt<T, T>();

    //        object, object

    //if (src.GetType() == typeof(object))
    //            return src;
    //        return src.Adapt(src.GetType(), src.GetType());
        }
    }
}
