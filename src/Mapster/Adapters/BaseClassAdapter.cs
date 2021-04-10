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

        protected ClassMapping CreateClassConverter(Expression source, ClassModel classModel, CompileArgument arg, Expression? destination = null)
        {
            var destinationMembers = classModel.Members;
            var unmappedDestinationMembers = new List<string>();
            var properties = new List<MemberMapping>();

            var sources = new List<Expression> {source};
            sources.AddRange(
                arg.Settings.ExtraSources.Select(src =>
                    src is LambdaExpression lambda 
                        ? lambda.Apply(arg.MapType, source) 
                        : ExpressionEx.PropertyOrFieldPath(source, (string)src)));
            foreach (var destinationMember in destinationMembers)
            {
                if (ProcessIgnores(arg, destinationMember, out var ignore))
                    continue;

                var resolvers = arg.Settings.ValueAccessingStrategies.AsEnumerable();
                if (arg.Settings.IgnoreNonMapped == true)
                    resolvers = resolvers.Where(ValueAccessingStrategy.CustomResolvers.Contains);
                var getter = (from fn in resolvers
                        from src in sources
                        select fn(src, destinationMember, arg))
                    .FirstOrDefault(result => result != null);

                var nextIgnore = arg.Settings.Ignore.Next((ParameterExpression)source, (ParameterExpression?)destination, destinationMember.Name);
                var nextResolvers = arg.Settings.Resolvers.Next(arg.Settings.Ignore, (ParameterExpression)source, destinationMember.Name)
                    .ToList();

                var propertyModel = new MemberMapping
                {
                    DestinationMember = destinationMember,
                    Ignore = ignore,
                    NextResolvers = nextResolvers,
                    NextIgnore = nextIgnore,
                    Source = (ParameterExpression)source,
                    Destination = (ParameterExpression?)destination,
                    UseDestinationValue = arg.MapType != MapType.Projection && destinationMember.UseDestinationValue(arg),
                };
                if (getter != null)
                {
                    propertyModel.Getter = arg.MapType == MapType.Projection 
                        ? getter 
                        : getter.ApplyNullPropagation();
                    properties.Add(propertyModel);
                }
                else
                {
                    if (arg.Settings.IgnoreNonMapped != true &&
                        arg.Settings.Unflattening == true &&
                        arg.DestinationType.GetDictionaryType() == null &&
                        arg.SourceType.GetDictionaryType() == null)
                    {
                        var extra = ValueAccessingStrategy.FindUnflatteningPairs(source, destinationMember, arg)
                            .Next(arg.Settings.Ignore, (ParameterExpression)source, destinationMember.Name);
                        nextResolvers.AddRange(extra);
                    }

                    if (classModel.ConstructorInfo != null)
                    {
                        var info = (ParameterInfo)destinationMember.Info!;
                        if (!info.IsOptional)
                        {
                            if (classModel.BreakOnUnmatched)
                                return null!;
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
                            return null!;
                        unmappedDestinationMembers.Add(destinationMember.Name);
                    }
                }
            }

            var requireDestMemberSource = arg.Settings.RequireDestinationMemberSource ??
                                          arg.Context.Config.RequireDestinationMemberSource;
            if (requireDestMemberSource &&
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
            out IgnoreDictionary.IgnoreItem ignore)
        {
            ignore = new IgnoreDictionary.IgnoreItem();
            if (!destinationMember.ShouldMapMember(arg, MemberSide.Destination))
                return true;

            return arg.Settings.Ignore.TryGetValue(destinationMember.Name, out ignore)
                   && ignore.Condition == null;
        }

        protected Expression CreateInstantiationExpression(Expression source, ClassMapping classConverter, CompileArgument arg)
        {
            var members = classConverter.Members;

            var arguments = new List<Expression>();
            foreach (var member in members)
            {
                var parameterInfo = (ParameterInfo)member.DestinationMember.Info!;
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
                    if (member.Ignore.Condition != null)
                    {
                        var body = member.Ignore.IsChildPath
                            ? member.Ignore.Condition.Body
                            : member.Ignore.Condition.Apply(arg.MapType, source, arg.DestinationType.CreateDefault());
                        var condition = ExpressionEx.Not(body);
                        getter = Expression.Condition(condition, getter, defaultConst);
                    }
                }
                arguments.Add(getter);
            }

            return Expression.New(classConverter.ConstructorInfo!, arguments);
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
                Members = arg.DestinationType.GetFieldsAndProperties(true)
            };
        }

        #endregion
    }
}
