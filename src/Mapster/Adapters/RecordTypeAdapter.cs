using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Mapster.Models;
using Mapster.Utils;

namespace Mapster.Adapters
{
    internal class RecordTypeAdapter : BaseClassAdapter
    {
        protected override int Score => -151;

        protected override bool CanMap(Type sourceType, Type destinationType, MapType mapType)
        {
            if (sourceType == typeof (string) || sourceType == typeof (object))
                return false;

            if (!destinationType.IsRecordType())
                return false;

            return true;
        }

        protected override Expression CreateInstantiationExpression(Expression source, CompileArgument arg)
        {
            //new TDestination(src.Prop1, src.Prop2)

            if (arg.Settings.ConstructUsingFactory != null)
                return base.CreateInstantiationExpression(source, arg);

            var classConverter = CreateClassConverter(source, null, arg);
            var properties = classConverter.Members;

            var arguments = new List<Expression>();
            foreach (var property in properties)
            {
                var parameterInfo = (ParameterInfo) property.SetterInfo;
                var defaultValue = parameterInfo.IsOptional ? parameterInfo.DefaultValue : parameterInfo.ParameterType.GetDefault();
                var defaultConst = Expression.Constant(defaultValue, property.Setter.Type);

                Expression getter;
                if (property.Getter == null)
                {
                    getter = defaultConst;
                }
                else
                {
                    getter = CreateAdaptExpression(property.Getter, property.Setter.Type, arg);

                    if (arg.Settings.IgnoreNullValues == true && (!property.Getter.Type.GetTypeInfo().IsValueType || property.Getter.Type.IsNullable()))
                    {
                        var condition = Expression.NotEqual(property.Getter, Expression.Constant(null, property.Getter.Type));
                        getter = Expression.Condition(condition, getter, defaultConst);
                    }
                    if (property.SetterCondition != null)
                    {
                        var condition = Expression.Not(property.SetterCondition.Apply(source, Expression.Constant(arg.DestinationType.GetDefault(), arg.DestinationType)));
                        getter = Expression.Condition(condition, getter, defaultConst);
                    }
                }
                arguments.Add(getter);
            }

            return Expression.New(classConverter.ConstructorInfo, arguments);
        }

        protected override Expression CreateBlockExpression(Expression source, Expression destination, CompileArgument arg)
        {
            return Expression.Empty();
        }

        protected override Expression CreateInlineExpression(Expression source, CompileArgument arg)
        {
            return CreateInstantiationExpression(source, arg);
        }

        protected override ClassModel GetClassModel(Type destinationType)
        {
            var props = destinationType.GetFieldsAndProperties();
            var ctor = destinationType.GetConstructors()[0];
            return new ClassModel
            {
                ConstructorInfo = ctor,
                Members = ctor.GetParameters().Select(ReflectionUtils.CreateModel)
            };
        }

    }

}
