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
    internal class ClassAdapter : BaseAdapter
    {
        public override int? Priority(Type sourceType, Type destinationType, MapType mapType)
        {
            if (sourceType == typeof (string) || sourceType == typeof (object))
                return null;

            var destProp = destinationType.GetPublicFieldsAndProperties(allowNoSetter: false).Select(x => x.Name).ToList();
            if (destProp.Count == 0)
                return null;

            return -150;
        }

        protected override Expression CreateExpressionBody(Expression source, Expression destination, CompileArgument arg)
        {
            if (arg.Context.Config.RequireExplicitMapping 
                && !arg.Context.Config.Dict.ContainsKey(new TypeTuple(arg.SourceType, arg.DestinationType)))
            {
                throw new InvalidOperationException(
                    $"Implicit mapping is not allowed (check GlobalSettings.RequireExplicitMapping) and no configuration exists for the following mapping: TSource: {arg.SourceType} TDestination: {arg.DestinationType}");
            }

            return base.CreateExpressionBody(source, destination, arg);
        }

        protected override Expression CreateBlockExpression(Expression source, Expression destination, CompileArgument arg)
        {
            //### !IgnoreNullValues
            //dest.Prop1 = convert(src.Prop1);
            //dest.Prop2 = convert(src.Prop2);

            //### IgnoreNullValues
            //if (src.Prop1 != null)
            //  dest.Prop1 = convert(src.Prop1);
            //if (src.Prop2 != null)
            //  dest.Prop2 = convert(src.Prop2);

            var properties = CreateAdapterModel(source, destination, arg);

            var lines = new List<Expression>();
            foreach (var property in properties)
            {
                var getter = CreateAdaptExpression(property.Getter, property.Setter.Type, arg);

                Expression itemAssign = Expression.Assign(property.Setter, getter);
                if (arg.Settings.IgnoreNullValues == true && (!property.Getter.Type.GetTypeInfo().IsValueType || property.Getter.Type.IsNullable()))
                {
                    var condition = Expression.NotEqual(property.Getter, Expression.Constant(null, property.Getter.Type));
                    itemAssign = Expression.IfThen(condition, itemAssign);
                }
                lines.Add(itemAssign);
            }

            return Expression.Block(lines);
        }

        protected override Expression CreateInlineExpression(Expression source, CompileArgument arg)
        {
            //new TDestination {
            //  Prop1 = convert(src.Prop1),
            //  Prop2 = convert(src.Prop2),
            //}

            var exp = CreateInstantiationExpression(source, arg);
            var memberInit = exp as MemberInitExpression;
            var newInstance = memberInit != null ? memberInit.NewExpression : (NewExpression)exp;
            var properties = CreateAdapterModel(source, newInstance, arg);

            var lines = new List<MemberBinding>();
            if (memberInit != null)
                lines.AddRange(memberInit.Bindings);
            foreach (var property in properties)
            {
                var getter = CreateAdaptExpression(property.Getter, property.Setter.Type, arg);
                var bind = Expression.Bind(property.SetterProperty, getter);
                lines.Add(bind);
            }

            return Expression.MemberInit(newInstance, lines);
        }

        #region Build the Adapter Model

        private static IEnumerable<PropertyModel> CreateAdapterModel(Expression source, Expression destination, CompileArgument arg)
        {
            Type sourceType = source.Type;
            var destinationMembers = arg.DestinationType.GetPublicFieldsAndProperties(allowNoSetter: false);

            var unmappedDestinationMembers = new List<string>();

            var properties = new List<PropertyModel>();

            for (int i = 0; i < destinationMembers.Count; i++)
            {
                MemberInfo destinationMember = destinationMembers[i];
                bool isProperty = destinationMember is PropertyInfo;

                if (ProcessIgnores(arg.Settings, destinationMember)) continue;

                if (ProcessCustomResolvers(source, destination, arg.Settings, destinationMember, properties)) continue;

                MemberInfo sourceMember = ReflectionUtils.GetPublicFieldOrProperty(sourceType, isProperty, destinationMember.Name);
                if (sourceMember != null)
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
                        SetterProperty = destinationMember,
                    };
                    properties.Add(propertyModel);
                }
                else
                {
                    if (FlattenMethod(source, destination, destinationMember, properties)) continue;

                    if (FlattenClass(source, destination, destinationMember, properties, arg.MapType == MapType.Projection)) continue;

                    if (destinationMember.HasPublicSetter())
                    {
                        unmappedDestinationMembers.Add(destinationMember.Name);
                    }
                }
            }

            if (arg.Context.Config.RequireDestinationMemberSource && unmappedDestinationMembers.Count > 0)
            {
                throw new ArgumentOutOfRangeException($"The following members of destination class {arg.DestinationType} do not have a corresponding source member mapped or ignored:{string.Join(",", unmappedDestinationMembers)}");
            }

            return properties;
        }

        private static bool FlattenClass(
            Expression source,
            Expression destination, 
            MemberInfo destinationMember,
            List<PropertyModel> properties,
            bool isProjection)
        {
            var getter = ReflectionUtils.GetDeepFlattening(source, destinationMember.Name, isProjection);
            if (getter != null)
            {
                var propertyModel = new PropertyModel
                {
                    ConvertType = 3,
                    Getter = getter,
                    Setter = destinationMember is PropertyInfo
                        ? Expression.Property(destination, (PropertyInfo) destinationMember)
                        : Expression.Field(destination, (FieldInfo) destinationMember),
                    SetterProperty = destinationMember,
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
                    SetterProperty = destinationMember,
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
                                SetterProperty = destinationMember,
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

        private static bool ProcessIgnores(TypeAdapterSettings config, MemberInfo destinationMember)
        {
            if (config.IgnoreMembers.Contains(destinationMember.Name))
                return true;
            var attributes = destinationMember.GetCustomAttributes(true).Select(attr => attr.GetType());
            return config.IgnoreAttributes.Overlaps(attributes);
        }

        #endregion
    }
}
