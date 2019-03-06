using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Mapster.Models;
using Mapster.Utils;

namespace Mapster.Adapters
{
    internal abstract class BaseClassAdapter : BaseAdapter
    {
        protected override bool CheckExplicitMapping => true;
        protected override bool UseTargetValue => true;

        #region Build the Adapter Model

        protected ClassMapping CreateClassConverter(Expression source, ClassModel classModel, CompileArgument arg)
        {
            var destinationMembers = classModel.Members;
            var unmappedDestinationMembers = new List<string>();
            var properties = new List<MemberMapping>();
            var includeResolvers = arg.Settings.Resolvers
                .Where(it => it.DestinationMemberName == "")
                .ToList();

            foreach (var destinationMember in destinationMembers)
            {
                if (ProcessIgnores(arg, destinationMember, out var setterCondition))
                    continue;

                var resolvers = arg.Settings.ValueAccessingStrategies.AsEnumerable();
                if (arg.Settings.IgnoreNonMapped == true)
                    resolvers = resolvers.Where(ValueAccessingStrategy.CustomResolvers.Contains);
                var getter = resolvers
                    .Select(fn => fn(source, destinationMember, arg))
                    .FirstOrDefault(result => result != null);

                if (getter != null)
                {
                    var propertyModel = new MemberMapping
                    {
                        Getter = getter,
                        DestinationMember = destinationMember,
                        SetterCondition = setterCondition,
                    };
                    properties.Add(propertyModel);
                }
                else if (classModel.ConstructorInfo != null)
                {
                    var info = (ParameterInfo)destinationMember.Info;
                    if (!info.IsOptional)
                        return null;
                    var propertyModel = new MemberMapping
                    {
                        Getter = null,
                        DestinationMember = destinationMember,
                        SetterCondition = setterCondition,
                    };
                    properties.Add(propertyModel);
                }
                else if (destinationMember.SetterModifier != AccessModifier.None)
                {
                    unmappedDestinationMembers.Add(destinationMember.Name);
                }
            }

            if (arg.Context.Config.RequireDestinationMemberSource && unmappedDestinationMembers.Count > 0)
            {
                throw new InvalidOperationException($"The following members of destination class {arg.DestinationType} do not have a corresponding source member mapped or ignored:{string.Join(",", unmappedDestinationMembers)}");
            }

            return new ClassMapping
            {
                ConstructorInfo = classModel.ConstructorInfo,
                Members = properties,
            };
        }

        protected static bool ProcessIgnores(
            CompileArgument arg,
            IMemberModel destinationMember,
            out LambdaExpression condition)
        {
            condition = null;
            if (!destinationMember.ShouldMapMember(arg, MemberSide.Destination))
                return true;

            return arg.Settings.IgnoreIfs.TryGetValue(destinationMember.Name, out condition)
                   && condition == null;
        }


        protected Expression CreateInstantiationExpression(Expression source, ClassMapping classConverter, CompileArgument arg)
        {
            var members = classConverter.Members;

            var arguments = new List<Expression>();
            foreach (var member in members)
            {
                var parameterInfo = (ParameterInfo)member.DestinationMember.Info;
                var defaultConst = parameterInfo.IsOptional
                    ? Expression.Constant(parameterInfo.DefaultValue, member.DestinationMember.Type)
                    : parameterInfo.ParameterType.CreateDefault();

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
                        var condition = ExpressionEx.Not(member.SetterCondition.Apply(source, arg.DestinationType.CreateDefault()));
                        getter = Expression.Condition(condition, getter, defaultConst);
                    }
                }
                arguments.Add(getter);
            }

            return Expression.New(classConverter.ConstructorInfo, arguments);
        }

        protected ClassModel GetClassModel(ConstructorInfo ctor)
        {
            return new ClassModel
            {
                ConstructorInfo = ctor,
                Members = ctor.GetParameters().Select(ReflectionUtils.CreateModel)
            };
        }

        #endregion
    }
}
