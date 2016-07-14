using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Mapster.Models;

namespace Mapster.Adapters
{
    /// <summary>
    /// Maps one class to another.
    /// </summary>
    /// <remarks>The operations in this class must be extremely fast.  Make sure to benchmark before making **any** changes in here.  
    /// The core Adapt method is critically important to performance.
    /// </remarks>
    internal class ClassAdapter : BaseClassAdapter
    {
        protected override int Score => -150;

        protected override bool CanMap(Type sourceType, Type destinationType, MapType mapType)
        {
            if (sourceType == typeof(string) || sourceType == typeof(object))
                return false;

            if (!destinationType.IsPoco())
                return false;

            return true;
        }

        protected override bool CanInline(Expression source, Expression destination, CompileArgument arg)
        {
            if (!base.CanInline(source, destination, arg))
                return false;
            if (arg.MapType != MapType.Projection &&
                arg.Settings.IgnoreNullValues == true)
                return false;
            return true;
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

            var classConverter = CreateClassConverter(source, destination, arg);
            var properties = classConverter.Members;

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

            return lines.Any() ? (Expression)Expression.Block(lines) : Expression.Empty();
        }

        protected override Expression CreateInlineExpression(Expression source, CompileArgument arg)
        {
            //new TDestination {
            //  Prop1 = convert(src.Prop1),
            //  Prop2 = convert(src.Prop2),
            //}

            var exp = CreateInstantiationExpression(source, arg);
            var memberInit = exp as MemberInitExpression;
            var newInstance = memberInit?.NewExpression ?? (NewExpression)exp;

            var classConverter = CreateClassConverter(source, newInstance, arg);
            var properties = classConverter.Members;

            var lines = new List<MemberBinding>();
            if (memberInit != null)
                lines.AddRange(memberInit.Bindings);
            foreach (var property in properties)
            {
                var getter = CreateAdaptExpression(property.Getter, property.Setter.Type, arg);

                //special null property check for projection
                //if we don't set null to property, EF will create empty object
                //except collection type & complex type which cannot be null
                if (arg.MapType == MapType.Projection
                    && property.Getter.Type != property.Setter.Type
                    && !property.Getter.Type.IsCollection()
                    && !property.Setter.Type.IsCollection()
                    && property.Getter.Type.GetTypeInfo().GetCustomAttributes(true).All(attr => attr.GetType().Name != "ComplexTypeAttribute")
                    && (!property.Getter.Type.GetTypeInfo().IsValueType || property.Getter.Type.IsNullable()))
                {
                    var compareNull = Expression.Equal(property.Getter, Expression.Constant(null, property.Getter.Type));
                    getter = Expression.Condition(
                        compareNull,
                        Expression.Constant(property.Setter.Type.GetDefault(), property.Setter.Type),
                        getter);
                }
                var bind = Expression.Bind((MemberInfo)property.SetterInfo, getter);
                lines.Add(bind);
            }

            return Expression.MemberInit(newInstance, lines);
        }

        protected override ClassModel GetClassModel(Type destinationType)
        {
            return new ClassModel
            {
                Members = destinationType.GetPublicFieldsAndProperties(allowNoSetter: false)
            };
        }
    }
}
