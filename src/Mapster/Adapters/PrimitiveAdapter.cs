using System;
using System.Linq.Expressions;
using Mapster.Utils;
using System.Reflection;

namespace Mapster.Adapters
{
    internal class PrimitiveAdapter : BaseAdapter
    {
        public override int? Priority(Type sourceType, Type destinationType, MapType mapType)
        {
            return -200;
        }

        protected override Expression CreateExpressionBody(Expression source, Expression destination, CompileArgument arg)
        {
            Expression convert = source;
            var sourceType = arg.SourceType;
            var destinationType = arg.DestinationType;
            if (sourceType != destinationType)
            {
                if (arg.MapType == MapType.Projection)
                    convert = Expression.Convert(convert, destinationType);
                else
                {
                    if (sourceType.IsNullable())
                    {
                        convert = Expression.Convert(convert, sourceType.GetGenericArguments()[0]);
                    }
                    convert = ReflectionUtils.BuildUnderlyingTypeConvertExpression(convert, sourceType, destinationType);
                    if (convert.Type != destinationType)
                        convert = Expression.Convert(convert, destinationType);
                }
            }
            if (arg.MapType != MapType.Projection
                && (!arg.SourceType.GetTypeInfo().IsValueType || arg.SourceType.IsNullable()) 
                && destinationType.GetTypeInfo().IsValueType 
                && !destinationType.IsNullable())
            {
                var compareNull = Expression.Equal(source, Expression.Constant(null, sourceType));
                convert = Expression.Condition(compareNull, Expression.Constant(destinationType.GetDefault(), destinationType), convert);
            }

            return convert;
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
