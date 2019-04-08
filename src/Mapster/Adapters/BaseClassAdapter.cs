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
        protected override ObjectType ObjectType => ObjectType.Class;
        protected override bool UseTargetValue => true;

        #region Build the Adapter Model

        protected ClassMapping CreateClassConverter(Expression source, ClassModel classModel, CompileArgument arg, Expression destination = null)
        {
            var destinationMembers = classModel.Members;
            var unmappedDestinationMembers = new List<string>();
            var properties = new List<MemberMapping>();

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

                var nextIgnoreIfs = arg.Settings.IgnoreIfs.Next(destinationMember.Name);
                var nextResolvers = arg.Settings.Resolvers
                    .Where(it => !arg.Settings.IgnoreIfs.TryGetValue(it.DestinationMemberName, out var condition) || condition != null)
                    .Select(it => it.Next(destinationMember.Name))
                    .Where(it => it != null)
                    .ToList();

                var propertyModel = new MemberMapping
                {
                    Getter = getter,
                    DestinationMember = destinationMember,
                    SetterCondition = setterCondition,
                    Resolvers = nextResolvers,
                    IgnoreIfs = nextIgnoreIfs,
                    Source = (ParameterExpression) source,
                    Destination = (ParameterExpression) destination,
                };
                if (getter != null)
                {
                    properties.Add(propertyModel);
                }
                else
                {
                    if (arg.Settings.IgnoreNonMapped != true &&
                        arg.Settings.Unflattening == true &&
                        arg.DestinationType.GetDictionaryType() != null &&
                        arg.SourceType.GetDictionaryType() != null)
                    {
                        var extra = ValueAccessingStrategy.FindUnflatteningPairs(source, destinationMember, arg);
                        nextResolvers.AddRange(extra);
                    }

                    if (classModel.ConstructorInfo != null)
                    {
                        var info = (ParameterInfo) destinationMember.Info;
                        if (!info.IsOptional)
                        {
                            if (classModel.BreakOnUnmatched)
                                return null;
                            unmappedDestinationMembers.Add(destinationMember.Name);
                        }

                        properties.Add(propertyModel);
                    }
                    else if (propertyModel.HasSettings())
                    {
                        propertyModel.Getter = Expression.New(typeof(Never));
                        properties.Add(propertyModel);
                    }
                    else if (destinationMember.SetterModifier != AccessModifier.None)
                    {
                        if (classModel.BreakOnUnmatched)
                            return null;
                        unmappedDestinationMembers.Add(destinationMember.Name);
                    }
                }
            }

            if (arg.Context.Config.RequireDestinationMemberSource && 
                unmappedDestinationMembers.Count > 0 &&
                arg.Settings.SkipDestinationMemberCheck != true)
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
                    getter = CreateAdaptExpression(member.Getter, member.DestinationMember.Type, arg, member);

                    if (arg.Settings.IgnoreNullValues == true && member.Getter.Type.CanBeNull())
                    {
                        var condition = Expression.NotEqual(member.Getter, Expression.Constant(null, member.Getter.Type));
                        getter = Expression.Condition(condition, getter, defaultConst);
                    }
                    if (member.SetterCondition != null)
                    {
                        var condition = ExpressionEx.Not(member.SetterCondition.Apply(arg.MapType, source, arg.DestinationType.CreateDefault()));
                        getter = Expression.Condition(condition, getter, defaultConst);
                    }
                }
                arguments.Add(getter);
            }

            return Expression.New(classConverter.ConstructorInfo, arguments);
        }

        protected virtual ClassModel GetConstructorModel(ConstructorInfo ctor, bool breakOnUnmatched)
        {
            return new ClassModel
            {
                BreakOnUnmatched = breakOnUnmatched,
                ConstructorInfo = ctor,
                Members = ctor.GetParameters().Select(ReflectionUtils.CreateModel)
            };
        }

        protected virtual ClassModel GetSetterModel(CompileArgument arg)
        {
            return new ClassModel
            {
                Members = arg.DestinationType.GetFieldsAndProperties(requireSetter: true, accessorFlags: BindingFlags.NonPublic | BindingFlags.Public)
            };
        }

        #endregion
    }
}
