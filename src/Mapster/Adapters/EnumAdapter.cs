using Mapster.Utils;
using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Mapster.Adapters
{
    internal class EnumAdapter : PrimitiveAdapter
    {
        protected override int Score => -109;   //must do before StringAdapter

        protected override bool CanMap(PreCompileArgument arg)
        {
            return CanMap(arg.SourceType, arg.DestinationType)
                   || CanMap(arg.DestinationType, arg.SourceType);
        }

        static bool CanMap(Type type1, Type type2)
        {
            if (!type1.UnwrapNullable().GetTypeInfo().IsEnum)
                return false;

            type2 = type2.UnwrapNullable();
            return type2.GetTypeInfo().IsEnum
                   || type2 == typeof(string)
                   || type2 == typeof(byte)
                   || type2 == typeof(sbyte)
                   || type2 == typeof(short)
                   || type2 == typeof(ushort)
                   || type2 == typeof(int)
                   || type2 == typeof(uint)
                   || type2 == typeof(long)
                   || type2 == typeof(ulong);
        }

        protected override Expression ConvertType(Expression source, Type destinationType, CompileArgument arg)
        {
            var srcType = source.Type;
            if (destinationType == typeof(string))
            {
                var method = typeof(Enum<>).MakeGenericType(srcType).GetMethod("ToString", new[] { srcType });
                return Expression.Call(method!, source);
            }
            else if (srcType == typeof(string))
            {
                var method = typeof(Enum<>).MakeGenericType(destinationType).GetMethod("Parse", new[] { typeof(string) });
                return Expression.Call(method!, source);
            }
            else if (destinationType.GetTypeInfo().IsEnum && srcType.GetTypeInfo().IsEnum && arg.Settings.MapEnumByName == true)
            {
                var method = typeof(Enum<>).MakeGenericType(srcType).GetMethod("ToString", new[] { srcType });
                var tostring = Expression.Call(method!, source);
                var methodParse = typeof(Enum<>).MakeGenericType(destinationType).GetMethod("Parse", new[] { typeof(string) });
                return Expression.Call(methodParse!, tostring);
            }


            return base.ConvertType(source, destinationType, arg);
        }
    }
}
