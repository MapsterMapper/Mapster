﻿using Mapster.Utils;
using System.Linq;
using System.Linq.Expressions;

namespace Mapster.Adapters
{
    internal class ReadOnlyInterfaceAdapter : ClassAdapter
    {
        protected override int Score => -151;

        protected override bool CanMap(PreCompileArgument arg)
        {
            return arg.DestinationType.IsInterface;
        }

        protected override bool CanInline(Expression source, Expression? destination, CompileArgument arg)
        {
            if (base.CanInline(source, destination, arg))
                return true;
            else
                return false;
            
        }

        protected override Expression CreateInstantiationExpression(Expression source, Expression? destination, CompileArgument arg)
        {
            //new TDestination(src.Prop1, src.Prop2)
            var destintionType = arg.DestinationType;
            var props = destintionType.GetFieldsAndProperties().ToList();

            //interface with readonly props
            if (props.Any(p => p.SetterModifier != AccessModifier.Public))
            {
                if (arg.GetConstructUsing() != null)
                    return base.CreateInstantiationExpression(source, destination, arg);

                var destType = DynamicTypeGenerator.GetTypeForInterface(arg.DestinationType, arg.Settings.Includes.Count > 0);
                if (destType == null)
                    return base.CreateInstantiationExpression(source, destination, arg);
                var ctor = destType.GetConstructors()[0];
                var classModel = GetConstructorModel(ctor, false);
                var classConverter = CreateClassConverter(source, classModel, arg);
                return CreateInstantiationExpression(source, classConverter, arg);
            }
            else
                return base.CreateInstantiationExpression(source,destination, arg);
        }

    }
}
