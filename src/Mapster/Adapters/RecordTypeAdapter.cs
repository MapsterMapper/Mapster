using System.Linq.Expressions;
using System.Reflection;

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

            var ctor = arg.DestinationType.GetConstructors()[0];
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
