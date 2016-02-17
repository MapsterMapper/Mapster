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
        protected abstract ClassModel GetClassModel(Type destinationType);

        #region Build the Adapter Model

        protected ClassConverter CreateClassConverter(Expression source, Expression destination, CompileArgument arg)
        {
            Type sourceType = source.Type;
            var classModel = GetClassModel(arg.DestinationType);
            var destinationMembers = classModel.Members;

            var unmappedDestinationMembers = new List<string>();

            var properties = new List<MemberConverter>();

            for (int i = 0; i < destinationMembers.Count; i++)
            {
                var destinationMember = destinationMembers[i];

                if (ProcessIgnores(arg.Settings, destinationMember)) continue;
                if (ProcessCustomResolvers(source, destination, arg.Settings, destinationMember, properties)) continue;

                var sourceMember = ReflectionUtils.GetMemberModel(sourceType, destinationMember.Name);
                if (sourceMember != null)
                {
                    var propertyModel = new MemberConverter
                    {
                        ConvertType = 1,
                        Getter = sourceMember.GetExpression(source),
                        Setter = destinationMember.GetExpression(destination),
                        SetterInfo = destinationMember.Info,
                    };
                    properties.Add(propertyModel);
                }
                else
                {
                    if (arg.MapType != MapType.Projection && FlattenMethod(source, destination, destinationMember, properties)) continue;

                    if (FlattenClass(source, destination, destinationMember, properties, arg.MapType == MapType.Projection)) continue;

                    if (classModel.ConstructorInfo != null)
                    {
                        var propertyModel = new MemberConverter
                        {
                            ConvertType = 0,
                            Getter = null,
                            Setter = destinationMember.GetExpression(destination),
                            SetterInfo = destinationMember.Info,
                        };
                        properties.Add(propertyModel);
                        continue;
                    }

                    if (destinationMember.HasSetter)
                    {
                        unmappedDestinationMembers.Add(destinationMember.Name);
                    }
                }
            }

            if (arg.Context.Config.RequireDestinationMemberSource && unmappedDestinationMembers.Count > 0)
            {
                throw new ArgumentOutOfRangeException($"The following members of destination class {arg.DestinationType} do not have a corresponding source member mapped or ignored:{string.Join(",", unmappedDestinationMembers)}");
            }

            return new ClassConverter
            {
                ConstructorInfo = classModel.ConstructorInfo,
                Members = properties,
            };
        }

        private static bool FlattenClass(
            Expression source,
            Expression destination,
            IMemberModel destinationMember,
            List<MemberConverter> properties,
            bool isProjection)
        {
            var getter = ReflectionUtils.GetDeepFlattening(source, destinationMember.Name, isProjection);
            if (getter != null)
            {
                var propertyModel = new MemberConverter
                {
                    ConvertType = 3,
                    Getter = getter,
                    Setter = destinationMember.GetExpression(destination),
                    SetterInfo = destinationMember.Info,
                };
                properties.Add(propertyModel);

                return true;
            }
            return false;
        }

        private static bool FlattenMethod(
            Expression source,
            Expression destination,
            IMemberModel destinationMember,
            List<MemberConverter> properties)
        {
            var getMethod = source.Type.GetMethod(string.Concat("Get", destinationMember.Name));
            if (getMethod != null)
            {
                var propertyModel = new MemberConverter
                {
                    ConvertType = 2,
                    Getter = Expression.Call(source, getMethod),
                    Setter = destinationMember.GetExpression(destination),
                    SetterInfo = destinationMember.Info,
                };

                properties.Add(propertyModel);

                return true;
            }
            return false;
        }

        private static bool ProcessCustomResolvers(
            Expression source,
            Expression destination,
            TypeAdapterSettings config,
            IMemberModel destinationMember,
            List<MemberConverter> properties)
        {
            bool isAdded = false;
            var resolvers = config.Resolvers;
            if (resolvers != null && resolvers.Count > 0)
            {
                MemberConverter memberConverter = null;
                LambdaExpression lastCondition = null;
                for (int j = 0; j < resolvers.Count; j++)
                {
                    var resolver = resolvers[j];
                    if (destinationMember.Name.Equals(resolver.MemberName))
                    {
                        if (memberConverter == null)
                        {
                            memberConverter = new MemberConverter
                            {
                                ConvertType = 5,
                                Setter = destinationMember.GetExpression(destination),
                                SetterInfo = destinationMember.Info,
                            };
                            isAdded = true;
                        }

                        Expression invoke = resolver.Invoker.Apply(source);
                        memberConverter.Getter = lastCondition != null
                            ? Expression.Condition(lastCondition.Apply(source), memberConverter.Getter, invoke)
                            : invoke;
                        lastCondition = resolver.Condition;
                        if (resolver.Condition == null)
                            break;
                    }
                }
                if (memberConverter != null)
                {
                    if (lastCondition != null)
                        memberConverter.Getter = Expression.Condition(lastCondition.Apply(source), memberConverter.Getter, Expression.Constant(memberConverter.Getter.Type.GetDefault(), memberConverter.Getter.Type));
                    properties.Add(memberConverter);
                }
            }
            return isAdded;
        }

        private static bool ProcessIgnores(TypeAdapterSettings config, IMemberModel destinationMember)
        {
            if (config.IgnoreMembers.Contains(destinationMember.Name))
                return true;
            var attributes = destinationMember.GetCustomAttributes(true).Select(attr => attr.GetType());
            return config.IgnoreAttributes.Overlaps(attributes);
        }

        #endregion
    }
}
