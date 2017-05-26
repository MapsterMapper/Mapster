using Mapster.Utils;
using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Mapster.Adapters
{
    internal class StringAdapter : PrimitiveAdapter
    {
        protected override int Score => -110;
        protected override bool CheckExplicitMapping => false;

        protected override bool CanMap(PreCompileArgument arg)
        {
            return arg.SourceType == typeof(string) || arg.DestinationType == typeof(string);
        }

        protected override Expression CreateExpressionBody(Expression source, Expression destination, CompileArgument arg)
        {
            var sourceType = arg.SourceType;
            var destinationType = arg.DestinationType;

            if (sourceType == destinationType)
                return source;

            if (destinationType == typeof(string))
            {
                if (sourceType.GetTypeInfo().IsEnum)
                {
                    var method = typeof(Enum<>).MakeGenericType(sourceType).GetMethod("ToString", new[] { sourceType });
                    return Expression.Call(method, source);
                }
                else
                {
                    var method = sourceType.GetMethod("ToString", Type.EmptyTypes);
                    return Expression.Call(source, method);
                }
            }
            else //if (sourceType == typeof(string))
            {
                if (destinationType.GetTypeInfo().IsEnum)
                {
                    var method = typeof(Enum<>).MakeGenericType(destinationType).GetMethod("Parse", new[] { typeof(string) });
                    return Expression.Call(method, source);
                }
                else
                {
                    var method = destinationType.GetMethod("Parse", new[] { typeof(string) });
                    if (method != null)
                        return Expression.Call(method, source);
                }
            }

            return base.ConvertType(source, arg);
        }

        protected override Expression CreateBlockExpression(Expression source, Expression destination, CompileArgument arg)
        {
            throw new NotImplementedException();
        }

        protected override Expression CreateInlineExpression(Expression source, CompileArgument arg)
        {
            throw new NotImplementedException();
        }
    }
}
