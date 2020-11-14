using System;
using System.Linq.Expressions;

// ReSharper disable once RedundantUsingDirective
using System.Reflection;

namespace Mapster.Adapters
{
    internal class StringAdapter : PrimitiveAdapter
    {
        protected override int Score => -110;   //must do before all class adapters

        protected override bool CanMap(PreCompileArgument arg)
        {
            return arg.SourceType == typeof(string) || arg.DestinationType == typeof(string);
        }

        protected override Expression ConvertType(Expression source, Type destinationType, CompileArgument arg)
        {
            if (destinationType == typeof(string))
            {
                var method = source.Type.GetMethod("ToString", Type.EmptyTypes) 
                             ?? typeof(object).GetMethod("ToString", Type.EmptyTypes);
                return Expression.Call(source, method!);
            }
            else //if (sourceType == typeof(string))
            {
                var method = destinationType.GetMethod("Parse", new[] { typeof(string) });
                if (method != null)
                    return Expression.Call(method, source);
            }

            return base.ConvertType(source, destinationType, arg);
        }
    }
}
