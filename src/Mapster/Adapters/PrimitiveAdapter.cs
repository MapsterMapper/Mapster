using Mapster.Utils;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Mapster.Adapters
{
    internal class PrimitiveAdapter : BaseAdapter
    {
        protected override int Score => -200;
        protected override bool CheckExplicitMapping => false;

        protected override bool CanMap(PreCompileArgument arg)
        {
            return true;
        }

        protected override Expression CreateExpressionBody(Expression source, Expression destination, CompileArgument arg)
        {
            Expression convert = source;
            var sourceType = arg.SourceType;
            var destinationType = arg.DestinationType;
            if (sourceType != destinationType)
            {
                if (sourceType.IsNullable())
                {
                    convert = Expression.Convert(convert, sourceType.GetGenericArguments()[0]);
                }
                convert = ConvertType(convert, arg);
                if (convert.Type != destinationType)
                    convert = Expression.Convert(convert, destinationType);

                if (arg.MapType != MapType.Projection
                    && (!arg.SourceType.GetTypeInfo().IsValueType || arg.SourceType.IsNullable()))
                {
                    //src == null ? default(TDestination) : convert(src)
                    var compareNull = Expression.Equal(source, Expression.Constant(null, sourceType));
                    convert = Expression.Condition(compareNull, Expression.Constant(destinationType.GetDefault(), destinationType), convert);
                }
            }

            return convert;
        }

        protected virtual Expression ConvertType(Expression source, CompileArgument arg)
        {
            var srcType = arg.SourceType.UnwrapNullable();
            var destType = arg.DestinationType.UnwrapNullable();

            if (srcType == destType)
                return source;

            if (destType.GetTypeInfo().IsEnum && srcType.GetTypeInfo().IsEnum && arg.Settings.MapEnumByName == true)
            {
                var method = typeof(Enum<>).MakeGenericType(srcType).GetMethod("ToString", new[] { srcType });
                var tostring = Expression.Call(method, source);
                var methodParse = typeof(Enum<>).MakeGenericType(destType).GetMethod("Parse", new[] { typeof(string) });

                return Expression.Call(methodParse, tostring);
            }

            if (IsObjectToPrimitiveConversion(srcType, destType))
            {
                return CreateConvertMethod(_primitiveTypes[destType], srcType, destType, source);
            }

            //try using type casting
            try
            {
                return Expression.Convert(source, destType);
            }
            catch
            {
                // ignored
            }

            if (!srcType.IsConvertible())
                throw new InvalidOperationException("Cannot convert immutable type, please consider using 'MapWith' method to create mapping");

            //using Convert
            if (_primitiveTypes.ContainsKey(destType))
            {
                return CreateConvertMethod(_primitiveTypes[destType], srcType, destType, source);
            }

            var changeTypeMethod = typeof(Convert).GetMethod("ChangeType", new[] { typeof(object), typeof(Type) });
            return Expression.Convert(Expression.Call(changeTypeMethod, Expression.Convert(source, typeof(object)), Expression.Constant(destType)), destType);
        }

        private static bool IsObjectToPrimitiveConversion(Type sourceType, Type destinationType)
        {
            return (sourceType == typeof(object)) && _primitiveTypes.ContainsKey(destinationType);
        }

        private static Expression CreateConvertMethod(string name, Type srcType, Type destType, Expression source)
        {
            var method = typeof(Convert).GetMethod(name, new[] { srcType });
            if (method != null)
                return Expression.Call(method, source);

            method = typeof(Convert).GetMethod(name, new[] { typeof(object) });
            return Expression.Convert(Expression.Call(method, Expression.Convert(source, typeof(object))), destType);
        }

        protected override Expression CreateBlockExpression(Expression source, Expression destination, CompileArgument arg)
        {
            throw new NotImplementedException();
        }

        protected override Expression CreateInlineExpression(Expression source, CompileArgument arg)
        {
            throw new NotImplementedException();
        }

        // Primitive types with their conversion methods from System.Convert class.
        private static Dictionary<Type, string> _primitiveTypes = new Dictionary<Type, string>() {
            { typeof(bool), "ToBoolean" },
            { typeof(short), "ToInt16" },
            { typeof(int), "ToInt32" },
            { typeof(long), "ToInt64" },
            { typeof(float), "ToSingle" },
            { typeof(double), "ToDouble" },
            { typeof(decimal), "ToDecimal" },
            { typeof(ushort), "ToUInt16" },
            { typeof(uint), "ToUInt32" },
            { typeof(ulong), "ToUInt64" },
            { typeof(byte), "ToByte" },
            { typeof(sbyte), "ToSByte" },
            { typeof(DateTime), "ToDateTime" }
        };
    }
}
