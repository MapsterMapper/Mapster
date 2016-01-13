using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Mapster.Models;
using Mapster.Utils;

namespace Mapster.Adapters
{
    /// <summary>
    /// Maps one class to another.
    /// </summary>
    /// <remarks>The operations in this class must be extremely fast.  Make sure to benchmark before making **any** changes in here.  
    /// The core Adapt method is critically important to performance.
    /// </remarks>
    internal class ClassAdapter : BaseAdapter
    {
        public override bool CanAdapt(Type sourceType, Type destinationType)
        {
            var settings = TypeAdapter.GetSettings(sourceType, destinationType);
            var model = CreateAdapterModel(Expression.Parameter(sourceType), Expression.Parameter(destinationType), settings);
            return model.Properties.Count > 0;
        }

        protected override Expression CreateSetterExpression(ParameterExpression source, ParameterExpression destination, TypeAdapterConfigSettingsBase settings)
        {
            var model = CreateAdapterModel(source, destination, settings);

            if (TypeAdapterConfig.GlobalSettings.RequireDestinationMemberSource && model.UnmappedProperties.Count > 0)
            {
                throw new ArgumentOutOfRangeException($"The following members of destination class {destination.Type} do not have a corresponding source member mapped or ignored:{string.Join(",", model.UnmappedProperties)}");
            }

            var lines = new List<Expression>();
            //var pDepth = Expression.Variable(typeof(int));

            //if (TypeAdapterConfig.GlobalSettings.EnableMaxDepth)
            //    list2.Add(Expression.Assign(pDepth, Expression.Subtract(depth, Expression.Constant(1))));

            foreach (var property in model.Properties)
            {
                var getter = CreateAdaptExpression(property.Getter, property.Setter.Type, settings);

                Expression itemAssign = Expression.Assign(property.Setter, getter);
                if (settings?.IgnoreNullValues == true && (!property.Getter.Type.IsValueType || property.Getter.Type.IsNullable()))
                {
                    var condition = Expression.NotEqual(property.Getter, Expression.Constant(null, property.Getter.Type));
                    itemAssign = Expression.IfThen(condition, itemAssign);
                }
                lines.Add(itemAssign);
            }

            return Expression.Block(lines);
        }

        #region Build the Adapter Model

        private static AdapterModel CreateAdapterModel(Expression source, Expression destination, TypeAdapterConfigSettingsBase settings)
        {
            Type destinationType = destination.Type;
            Type sourceType = source.Type;
            List<MemberInfo> destinationMembers = destinationType.GetPublicFieldsAndProperties(allowNoSetter: false);
            if (destinationMembers.Count == 0)
                return AdapterModel.Default;

            var unmappedDestinationMembers = new List<string>();

            var properties = new List<PropertyModel>();

            for (int i = 0; i < destinationMembers.Count; i++)
            {
                MemberInfo destinationMember = destinationMembers[i];
                bool isProperty = destinationMember is PropertyInfo;

                if (settings != null)
                {
                    if (ProcessIgnores(settings, destinationMember)) continue;

                    if (ProcessCustomResolvers(source, destination, settings, destinationMember, properties)) continue;
                }

                MemberInfo sourceMember = ReflectionUtils.GetPublicFieldOrProperty(sourceType, isProperty, destinationMember.Name);
                if (sourceMember == null)
                {
                    if (FlattenMethod(source, destination, destinationMember, properties)) continue;

                    if (FlattenClass(source, destination, destinationMember, properties)) continue;

                    if (destinationMember.HasPublicSetter())
                    {
                        unmappedDestinationMembers.Add(destinationMember.Name);
                    }
                }
                else
                {
                    var propertyModel = new PropertyModel
                    {
                        ConvertType = 1,
                        Getter = sourceMember is PropertyInfo
                            ? Expression.Property(source, (PropertyInfo)sourceMember)
                            : Expression.Field(source, (FieldInfo)sourceMember),
                        Setter = destinationMember is PropertyInfo
                            ? Expression.Property(destination, (PropertyInfo) destinationMember)
                            : Expression.Field(destination, (FieldInfo) destinationMember),
                        SetterPropertyName = destinationMember.Name,
                    };
                    properties.Add(propertyModel);
                }
            }

            return new AdapterModel
            {
                Properties = properties,
                UnmappedProperties = unmappedDestinationMembers,
            };
        }

        private static bool FlattenClass(
            Expression source,
            Expression destination, 
            MemberInfo destinationMember,
            List<PropertyModel> properties)
        {
            var getter = ReflectionUtils.GetDeepFlattening(source, destinationMember.Name);
            if (getter != null)
            {
                var propertyModel = new PropertyModel
                {
                    ConvertType = 3,
                    Getter = getter,
                    Setter = destinationMember is PropertyInfo
                        ? Expression.Property(destination, (PropertyInfo) destinationMember)
                        : Expression.Field(destination, (FieldInfo) destinationMember),
                    SetterPropertyName = destinationMember.Name,
                };
                properties.Add(propertyModel);

                return true;
            }
            return false;
        }

        private static bool FlattenMethod(
            Expression source,
            Expression destination, 
            MemberInfo destinationMember,
            List<PropertyModel> properties)
        {
            var getMethod = source.Type.GetMethod(string.Concat("Get", destinationMember.Name));
            if (getMethod != null)
            {
                var propertyModel = new PropertyModel
                {
                    ConvertType = 2,
                    Getter = Expression.Call(source, getMethod),
                    Setter = destinationMember is PropertyInfo
                        ? Expression.Property(destination, (PropertyInfo)destinationMember)
                        : Expression.Field(destination, (FieldInfo)destinationMember),
                    SetterPropertyName = destinationMember.Name,
                };

                properties.Add(propertyModel);

                return true;
            }
            return false;
        }

        private static bool ProcessCustomResolvers(
            Expression source,
            Expression destination,
            TypeAdapterConfigSettingsBase config, 
            MemberInfo destinationMember,
            List<PropertyModel> properties)
        {
            bool isAdded = false;
            var resolvers = config.Resolvers;
            if (resolvers != null && resolvers.Count > 0)
            {
                PropertyModel propertyModel = null;
                LambdaExpression lastCondition = null;
                for (int j = 0; j < resolvers.Count; j++)
                {
                    var resolver = resolvers[j];
                    if (destinationMember.Name.Equals(resolver.MemberName))
                    {
                        if (propertyModel == null)
                        {
                            propertyModel = new PropertyModel
                            {
                                ConvertType = 5,
                                Setter = destinationMember is PropertyInfo
                                    ? Expression.Property(destination, (PropertyInfo) destinationMember)
                                    : Expression.Field(destination, (FieldInfo) destinationMember),
                                SetterPropertyName = destinationMember.Name,
                            };
                            isAdded = true;
                        }

                        Expression invoke = resolver.Invoker.Apply(source);
                        propertyModel.Getter = lastCondition != null
                            ? Expression.Condition(lastCondition.Apply(source), propertyModel.Getter, invoke)
                            : invoke;
                        lastCondition = resolver.Condition;
                        if (resolver.Condition == null)
                            break;
                    }
                }
                if (propertyModel != null)
                {
                    if (lastCondition != null)
                        propertyModel.Getter = Expression.Condition(lastCondition.Apply(source), propertyModel.Getter, Expression.Constant(propertyModel.Getter.Type.GetDefault(), propertyModel.Getter.Type));
                    properties.Add(propertyModel);
                }
            }
            return isAdded;
        }

        private static bool ProcessIgnores(TypeAdapterConfigSettingsBase config, MemberInfo destinationMember)
        {
            var ignoreMembers = config.IgnoreMembers;
            if (ignoreMembers != null && ignoreMembers.Count > 0)
            {
                bool ignored = false;
                for (int j = 0; j < ignoreMembers.Count; j++)
                {
                    if (destinationMember.Name.Equals(ignoreMembers[j]))
                    {
                        ignored = true;
                        break;
                    }
                }
                if (ignored)
                    return true;
            }
            return false;
        }

        #endregion
    }
}
