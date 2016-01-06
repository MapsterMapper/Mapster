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
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TDestination">The destination type</typeparam>
    public static class ClassAdapter<TSource, TDestination>
    {
        public static Expression<Func<int, TSource, TDestination>> CreateAdaptFunc()
        {
            var depth = Expression.Parameter(typeof(int));
            var p = Expression.Parameter(typeof(TSource));
            var body = CreateExpressionBody(depth, p, null);
            return Expression.Lambda<Func<int, TSource, TDestination>>(body, depth, p);
        }

        public static Expression<Func<int, TSource, TDestination, TDestination>> CreateAdaptTargetFunc()
        {
            var depth = Expression.Parameter(typeof(int));
            var p = Expression.Parameter(typeof(TSource));
            var p2 = Expression.Parameter(typeof(TDestination));
            var body = CreateExpressionBody(depth, p, p2);
            return Expression.Lambda<Func<int, TSource, TDestination, TDestination>>(body, depth, p, p2);
        }

        public static Expression CreateExpressionBody(ParameterExpression depth, ParameterExpression p, ParameterExpression p2)
        {
            var destinationType = typeof(TDestination);
            var list = new List<Expression>();
            var setting = TypeAdapterConfig<TSource, TDestination>.ConfigSettings;

            var pDest = Expression.Variable(destinationType);
            Expression assign;
            if (p2 != null)
            {
                assign = Expression.Assign(pDest, p2);
            }
            else if (setting?.ConstructUsing != null)
            {
                assign = Expression.Assign(pDest, Expression.Invoke(setting.ConstructUsing));
            }
            else
            {
                assign = Expression.Assign(pDest, Expression.New(destinationType));
            }

            var models = CreatePropertyModels(p, pDest);
            var list2 = new List<Expression>();
            list2.Add(assign);
            foreach (var model in models)
            {
                var typeAdaptType = typeof(TypeAdapter<,>).MakeGenericType(model.Getter.Type, model.Setter.Type);
                var adaptMethod = typeAdaptType.GetMethod("AdaptWithDepth",
                    new[] { typeof(int), model.Getter.Type });
                Expression itemAssign = model.Getter.Type == model.Setter.Type && setting?.NewInstanceForSameType == false
                    ? Expression.Assign(model.Setter, model.Getter)
                    : Expression.Assign(model.Setter, Expression.Call(adaptMethod, depth, model.Getter));
                if (setting?.IgnoreNullValues == true && (!model.Getter.Type.IsValueType || model.Getter.Type.IsNullable()))
                {
                    var condition = Expression.Equal(model.Getter, Expression.Constant(null, model.Getter.Type));
                    itemAssign = Expression.IfThen(condition, itemAssign);
                }
                list2.Add(itemAssign);
            }
            Expression set = Expression.Block(list2);

            if (setting?.MaxDepth != null && setting.MaxDepth.Value > 0)
            {
                var compareDepth = Expression.GreaterThan(depth, Expression.Constant(setting.MaxDepth.Value));
                set = Expression.IfThenElse(
                    compareDepth,
                    Expression.Assign(pDest, (Expression)p2 ?? Expression.Constant(null, destinationType)),
                    set);
            }

            var compareNull = Expression.Equal(p, Expression.Constant(null, p.Type));
            set = Expression.IfThenElse(
                compareNull,
                Expression.Assign(pDest, (Expression)p2 ?? Expression.Constant(null, destinationType)),
                set);
            list.Add(set);

            var destinationTransforms = setting != null
                ? setting.DestinationTransforms.Transforms
                : TypeAdapterConfig.GlobalSettings.DestinationTransforms.Transforms;
            if (destinationTransforms.ContainsKey(destinationType))
            {
                var transform = destinationTransforms[destinationType];
                var invoke = Expression.Invoke(transform, pDest);
                list.Add(Expression.Assign(pDest, invoke));
            }
            list.Add(pDest);

            return Expression.Block(new[] {pDest}, list);
        }

        #region Build the Adapter Model

        private static List<PropertyModel> CreatePropertyModels(Expression source, Expression destination)
        {
            Type destinationType = typeof(TDestination);
            Type sourceType = typeof(TSource);

            var unmappedDestinationMembers = new List<string>();

            var properties = new List<PropertyModel>();

            List<MemberInfo> destinationMembers = destinationType.GetPublicFieldsAndProperties(allowNoSetter: false);

            var settings = TypeAdapterConfig<TSource, TDestination>.ConfigSettings;

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

            if (TypeAdapterConfig.GlobalSettings.RequireDestinationMemberSource && unmappedDestinationMembers.Count > 0)
            {
                throw new ArgumentOutOfRangeException($"The following members of destination class {typeof (TDestination)} do not have a corresponding source member mapped or ignored:{string.Join(",", unmappedDestinationMembers)}");
            }

            return properties;
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
            TypeAdapterConfigSettings<TSource, TDestination> config, 
            MemberInfo destinationMember,
            List<PropertyModel> properties)
        {
            bool isAdded = false;
            var resolvers = config.Resolvers;
            if (resolvers != null && resolvers.Count > 0)
            {
                PropertyModel propertyModel = null;
                Expression lastCondition = null;
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

                        Expression invoke = Expression.Invoke(resolver.Invoker, source);
                        propertyModel.Getter = lastCondition != null
                            ? Expression.Condition(Expression.Invoke(lastCondition, source), propertyModel.Getter, invoke)
                            : invoke;
                        lastCondition = resolver.Condition;
                        if (resolver.Condition == null)
                            break;
                    }
                }
                if (propertyModel != null)
                {
                    if (lastCondition != null)
                        propertyModel.Getter = Expression.Condition(Expression.Invoke(lastCondition, source), propertyModel.Getter, Expression.Constant(propertyModel.Getter.Type.IsValueType && !propertyModel.Getter.Type.IsNullable() ? Activator.CreateInstance(propertyModel.Getter.Type) : null, propertyModel.Getter.Type));
                    properties.Add(propertyModel);
                }
            }
            return isAdded;
        }

        private static bool ProcessIgnores(TypeAdapterConfigSettings<TSource, TDestination> config, MemberInfo destinationMember)
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
