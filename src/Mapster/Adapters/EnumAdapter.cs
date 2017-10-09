using Mapster.Utils;
using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Mapster.Adapters
{
    internal class EnumAdapter : PrimitiveAdapter
    {
        protected override int Score => -109;   //must do before StringAdapter
        protected override bool CheckExplicitMapping => false;

        protected override bool CanMap(PreCompileArgument arg)
        {
            return arg.SourceType.UnwrapNullable().GetTypeInfo().IsEnum 
                || arg.DestinationType.UnwrapNullable().GetTypeInfo().IsEnum;
        }

        protected override Expression ConvertType(Expression source, Type destinationType, CompileArgument arg)
        {
            var srcType = source.Type;
            if (destinationType == typeof(string))
            {
                var method = typeof(Enum<>).MakeGenericType(srcType).GetMethod("ToString", new[] { srcType });
                return Expression.Call(method, source);
            }
            else if (srcType == typeof(string))
            {
                var method = typeof(Enum<>).MakeGenericType(destinationType).GetMethod("Parse", new[] { typeof(string) });
                return Expression.Call(method, source);
            }
            else if (destinationType.GetTypeInfo().IsEnum && srcType.GetTypeInfo().IsEnum && arg.Settings.MapEnumByName == true)
            {
                var method = typeof(Enum<>).MakeGenericType(srcType).GetMethod("ToString", new[] { srcType });
                var tostring = Expression.Call(method, source);
                var methodParse = typeof(Enum<>).MakeGenericType(destinationType).GetMethod("Parse", new[] { typeof(string) });

                return Expression.Call(methodParse, tostring);
            }


            return base.ConvertType(source, destinationType, arg);
        }
    }
}
