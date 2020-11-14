using Mapster.Utils;
using System;
using System.Linq.Expressions;

// ReSharper disable once RedundantUsingDirective
using System.Reflection;

namespace Mapster.Adapters
{
    internal class PrimitiveAdapter : BaseAdapter
    {
        protected override int Score => -200;   //must do last

        protected override bool CanMap(PreCompileArgument arg)
        {
            return true;
        }

        protected override Expression CreateExpressionBody(Expression source, Expression? destination, CompileArgument arg)
        {
            Expression convert = source;
            var sourceType = arg.SourceType;
            var destinationType = arg.DestinationType;
            if (sourceType != destinationType)
            {
                if (sourceType.IsNullable())
                    convert = Expression.Convert(convert, sourceType.GetGenericArguments()[0]);
                var destType = arg.DestinationType.UnwrapNullable();

                if (convert.Type != destType)
                    convert = ConvertType(convert, destType, arg);
                if (convert.Type != destinationType)
                    convert = Expression.Convert(convert, destinationType);

                if (arg.MapType != MapType.Projection && source.CanBeNull())
                {
                    //src == null ? default(TDestination) : convert(src)
                    var compareNull = Expression.Equal(source, Expression.Constant(null, sourceType));
                    convert = Expression.Condition(compareNull, destinationType.CreateDefault(), convert);
                }
            }

            return convert;
        }

        protected virtual Expression ConvertType(Expression source, Type destinationType, CompileArgument arg)
        {
            var srcType = source.Type;

            //try using type casting
            try
            {
                return Expression.Convert(source, destinationType);
            }
            catch
            {
                // ignored
            }

            if (!srcType.IsConvertible())
                throw new InvalidOperationException("Cannot convert immutable type, please consider using 'MapWith' method to create mapping");

            //using Convert
            var result = ReflectionUtils.CreateConvertMethod(srcType, destinationType, source);
            if (result != null)
                return result;

            var changeTypeMethod = typeof(Convert).GetMethod("ChangeType", new[] { typeof(object), typeof(Type) });
            return Expression.Convert(Expression.Call(changeTypeMethod!, Expression.Convert(source, typeof(object)), Expression.Constant(destinationType)), destinationType);
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
