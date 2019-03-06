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
        protected override int Score => -149;

        protected override bool CanMap(PreCompileArgument arg)
        {
            if (!arg.DestinationType.IsRecordType())
                return false;

            return true;
        }

        protected override Expression CreateInstantiationExpression(Expression source, Expression destination, CompileArgument arg)
        {
            //new TDestination(src.Prop1, src.Prop2)

            if (arg.Settings.ConstructUsingFactory != null)
                return base.CreateInstantiationExpression(source, destination, arg);

            var classConverter = CreateClassConverter(source, null, arg);
            var members = classConverter.Members;

            var arguments = new List<Expression>();
            foreach (var member in members)
            {
                var parameterInfo = (ParameterInfo)member.DestinationMember.Info;
                var defaultConst = parameterInfo.IsOptional
                    ? Expression.Constant(parameterInfo.DefaultValue, member.DestinationMember.Type)
                    : parameterInfo.ParameterType.CreateDefault(arg.MapType);

                Expression getter;
                if (member.Getter == null)
                {
                    getter = defaultConst;
                }
                else
                {
                    getter = CreateAdaptExpression(member.Getter, member.DestinationMember.Type, arg);

                    if (arg.Settings.IgnoreNullValues == true && member.Getter.Type.CanBeNull())
                    {
                        var condition = Expression.NotEqual(member.Getter, Expression.Constant(null, member.Getter.Type));
                        getter = Expression.Condition(condition, getter, defaultConst);
                    }
                    if (member.SetterCondition != null)
                    {
                        var condition = Expression.Not(member.SetterCondition.Apply(source, arg.DestinationType.CreateDefault(arg.MapType)));
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

        protected override ClassModel GetClassModel(Type destinationType, CompileArgument arg)
        {
            var ctor = destinationType.GetConstructors()[0];
            return new ClassModel
            {
                ConstructorInfo = ctor,
                Members = ctor.GetParameters().Select(ReflectionUtils.CreateModel)
            };
        }

    }

}
