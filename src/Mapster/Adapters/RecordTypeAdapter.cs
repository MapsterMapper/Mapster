using System.Linq.Expressions;
using System.Reflection;
using Mapster.Utils;

namespace Mapster.Adapters
{
    internal class RecordTypeAdapter : BaseClassAdapter
    {
        protected override int Score => -149;
        protected override bool UseTargetValue => false;

        protected override bool CanMap(PreCompileArgument arg)
        {
            return arg.DestinationType.IsRecordType();
        }

        protected override Expression CreateInstantiationExpression(Expression source, Expression? destination, CompileArgument arg)
        {
            //new TDestination(src.Prop1, src.Prop2)

            if (arg.GetConstructUsing() != null)
                return base.CreateInstantiationExpression(source, destination, arg);

            var destType = arg.DestinationType.GetTypeInfo().IsInterface
                ? DynamicTypeGenerator.GetTypeForInterface(arg.DestinationType, arg.Settings.Includes.Count > 0)
                : arg.DestinationType;
            if (destType == null)
                return base.CreateInstantiationExpression(source, destination, arg);
            var ctor = destType.GetConstructors()[0];
            var classModel = GetConstructorModel(ctor, false);
            var classConverter = CreateClassConverter(source, classModel, arg);
            return CreateInstantiationExpression(source, classConverter, arg);
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
