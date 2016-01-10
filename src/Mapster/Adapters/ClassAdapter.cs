using System;
using System.Collections.Generic;
using System.Linq;
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
    internal class ClassAdapter : ITypeAdapterWithTarget
    {
        public bool CanAdapt(Type sourceType, Type destinationType)
        {
            var settings = TypeAdapter.GetSettings(sourceType, destinationType);
            var model = CreateAdapterModel(Expression.Parameter(sourceType), Expression.Parameter(destinationType), settings);
            return model.Properties.Count > 0;
        }

        public Func<MapContext, TSource, TDestination> CreateAdaptFunc<TSource, TDestination>()
        {
            //var depth = Expression.Parameter(typeof(int));
            var context = Expression.Parameter(typeof(MapContext));
            var p = Expression.Parameter(typeof(TSource));
            var settings = TypeAdapterConfig<TSource, TDestination>.ConfigSettings;
            var body = CreateExpressionBody(context, p, null, typeof(TSource), typeof(TDestination), settings);
            return Expression.Lambda<Func<MapContext, TSource, TDestination>>(body, context, p).Compile();
        }

        public Func<MapContext, TSource, TDestination, TDestination> CreateAdaptTargetFunc<TSource, TDestination>()
        {
            //var depth = Expression.Parameter(typeof(int));
            var context = Expression.Parameter(typeof(MapContext));
            var p = Expression.Parameter(typeof(TSource));
            var p2 = Expression.Parameter(typeof(TDestination));
            var settings = TypeAdapterConfig<TSource, TDestination>.ConfigSettings;
            var body = CreateExpressionBody(context, p, p2, typeof(TSource), typeof(TDestination), settings);
            return Expression.Lambda<Func<MapContext, TSource, TDestination, TDestination>>(body, context, p, p2).Compile();
        }

        private static Expression CreateExpressionBody(ParameterExpression context, ParameterExpression p, ParameterExpression p2, Type sourceType, Type destinationType, TypeAdapterConfigSettingsBase settings)
        {
            var list = new List<Expression>();

            var pDest = Expression.Variable(destinationType);
            Expression assign;
            if (p2 != null)
            {
                assign = Expression.Assign(pDest, p2);
            }
            else if (settings?.ConstructUsing != null)
            {
                assign = Expression.Assign(pDest, settings.ConstructUsing.Body);
            }
            else
            {
                assign = Expression.Assign(pDest, Expression.New(destinationType));
            }

            var localTransform = settings?.DestinationTransforms.Transforms;
            var model = CreateAdapterModel(p, pDest, settings);

            if (TypeAdapterConfig.GlobalSettings.RequireDestinationMemberSource && model.UnmappedProperties.Count > 0)
            {
                throw new ArgumentOutOfRangeException($"The following members of destination class {destinationType} do not have a corresponding source member mapped or ignored:{string.Join(",", model.UnmappedProperties)}");
            }

            var list2 = new List<Expression>();
            //var pDepth = Expression.Variable(typeof(int));

            //if (TypeAdapterConfig.GlobalSettings.EnableMaxDepth)
            //    list2.Add(Expression.Assign(pDepth, Expression.Subtract(depth, Expression.Constant(1))));

            foreach (var property in model.Properties)
            {
                var adapter = TypeAdapter.GetAdapter(property.Getter.Type, property.Setter.Type) as ITypeExpression;

                Expression getter;
                if (adapter != null)
                {
                    getter = adapter.CreateExpression(property.Getter, property.Getter.Type, property.Setter.Type);
                }
                else
                {
                    var typeAdaptType = typeof (TypeAdapter<,>).MakeGenericType(property.Getter.Type, property.Setter.Type);
                    var adaptMethod = typeAdaptType.GetMethod("AdaptWithContext",
                        new[] {typeof(MapContext), property.Getter.Type});
                    getter = property.Getter.Type == property.Setter.Type && settings?.SameInstanceForSameType == true
                        ? property.Getter
                        : Expression.Call(adaptMethod, context, property.Getter);
                }

                if (localTransform != null && localTransform.ContainsKey(getter.Type))
                    getter = Expression.Invoke(localTransform[getter.Type], getter);

                Expression itemAssign = Expression.Assign(property.Setter, getter);
                if (settings?.IgnoreNullValues == true && (!property.Getter.Type.IsValueType || property.Getter.Type.IsNullable()))
                {
                    var condition = Expression.NotEqual(property.Getter, Expression.Constant(null, property.Getter.Type));
                    itemAssign = Expression.IfThen(condition, itemAssign);
                }
                list2.Add(itemAssign);
            }

            Expression set;
            if ((TypeAdapterConfig.GlobalSettings.PreserveReference || settings?.PreserveReference == true) && 
                !sourceType.IsValueType && 
                !destinationType.IsValueType)
            {
                var refDict = Expression.Property(context, "References");
                var refAdd = Expression.Call(refDict, "Add", null, Expression.Convert(p, typeof(object)), Expression.Convert(pDest, typeof(object)));
                set = Expression.Block(new[] {assign, refAdd}.Concat(list2));

                var pDest3 = Expression.Variable(typeof(object));
                var tryGetMethod = typeof(Dictionary<object, object>).GetMethod("TryGetValue", new[] { typeof(object), typeof(object).MakeByRefType() });
                var checkHasRef = Expression.Call(refDict, tryGetMethod, p, pDest3);
                set = Expression.IfThenElse(
                    checkHasRef,
                    ExpressionEx.Assign(pDest, pDest3),
                    set);
                set = Expression.Block(new[] { pDest3 }, set);
            }
            else
            {
                set = Expression.Block(new[] {assign}.Concat(list2));
            }

            //if (TypeAdapterConfig.GlobalSettings.EnableMaxDepth)
            //{
            //    var compareDepth = Expression.Equal(depth, Expression.Constant(0));
            //    set = Expression.IfThenElse(
            //        compareDepth,
            //        Expression.Assign(pDest, (Expression) p2 ?? Expression.Constant(null, destinationType)),
            //        set);
            //}

            if (!sourceType.IsValueType || sourceType.IsNullable())
            {
                var compareNull = Expression.Equal(p, Expression.Constant(null, p.Type));
                set = Expression.IfThenElse(
                    compareNull,
                    Expression.Assign(pDest, (Expression) p2 ?? Expression.Constant(destinationType.GetDefault(), destinationType)),
                    set);
            }
            list.Add(set);

            var destinationTransforms = TypeAdapterConfig.GlobalSettings.DestinationTransforms.Transforms;
            if (destinationTransforms.ContainsKey(destinationType))
            {
                var transform = destinationTransforms[destinationType];
                var invoke = Expression.Invoke(transform, pDest);
                list.Add(Expression.Assign(pDest, invoke));
            }
            if (localTransform != null && localTransform.ContainsKey(destinationType))
            {
                var transform = localTransform[destinationType];
                var invoke = Expression.Invoke(transform, pDest);
                list.Add(Expression.Assign(pDest, invoke));
            }

            list.Add(pDest);

            return Expression.Block(new[] {pDest}, list);
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
