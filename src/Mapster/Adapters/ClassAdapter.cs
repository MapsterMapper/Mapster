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
    internal class ClassAdapter : BaseClassAdapter
    {
        protected override int Score => -150;

        protected override bool CanMap(PreCompileArgument arg)
        {
            return arg.ExplicitMapping || arg.DestinationType.IsPoco();
        }

        protected override bool CanInline(Expression source, Expression destination, CompileArgument arg)
        {
            if (!base.CanInline(source, destination, arg))
                return false;

            if (arg.MapType == MapType.MapToTarget)
                return false;
            var constructUsing = arg.GetConstructUsing();
            if (constructUsing != null &&
                constructUsing.Body.NodeType != ExpressionType.New &&
                constructUsing.Body.NodeType != ExpressionType.MemberInit)
            {
                if (arg.MapType == MapType.Projection)
                    throw new InvalidOperationException("ConstructUsing for projection is support only New and MemberInit expression.");
                return false;
            }

            //IgnoreIfs, IgnoreNullValue isn't supported by projection
            if (arg.MapType == MapType.Projection)
                return true;
            if (arg.Settings.IgnoreNullValues == true)
                return false;
            if (arg.Settings.IgnoreIfs.Any(item => item.Value != null))
                return false;
            return true;
        }

        protected override Expression CreateInstantiationExpression(Expression source, Expression destination, CompileArgument arg)
        {
            //new TDestination(src.Prop1, src.Prop2)

            if (arg.GetConstructUsing() != null || arg.Settings.MapToConstructor != true)
                return base.CreateInstantiationExpression(source, destination, arg);

            var classConverter = arg.DestinationType.GetConstructors()
                .OrderByDescending(it => it.GetParameters().Length)
                .Select(GetClassModel)
                .Select(it => CreateClassConverter(source, it, arg))
                .FirstOrDefault(it => it != null);

            if (classConverter == null)
                throw new Exception();

            return CreateInstantiationExpression(source, classConverter, arg);
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

            var classModel = GetClassModel(arg.DestinationType);
            var classConverter = CreateClassConverter(source, classModel, arg);
            var members = classConverter.Members;

            var lines = new List<Expression>();
            Dictionary<LambdaExpression, List<Expression>> conditions = null;
            foreach (var member in members)
            {
                var value = arg.MapType == MapType.MapToTarget
                    ? CreateAdaptToExpression(member.Getter, member.DestinationMember.GetExpression(destination), arg)
                    : CreateAdaptExpression(member.Getter, member.DestinationMember.Type, arg);

                Expression itemAssign = member.DestinationMember.SetExpression(destination, value);
                if (arg.Settings.IgnoreNullValues == true && member.Getter.Type.CanBeNull())
                {
                    var condition = Expression.NotEqual(member.Getter, Expression.Constant(null, member.Getter.Type));
                    itemAssign = Expression.IfThen(condition, itemAssign);
                }

                if (member.SetterCondition != null)
                {
                    if (conditions == null)
                        conditions = new Dictionary<LambdaExpression, List<Expression>>();
                    if (!conditions.TryGetValue(member.SetterCondition, out List<Expression> pendingAssign))
                    {
                        pendingAssign = new List<Expression>();
                        conditions[member.SetterCondition] = pendingAssign;
                    }
                    pendingAssign.Add(itemAssign);
                }
                else
                {
                    lines.Add(itemAssign);
                }
            }

            if (conditions != null)
            {
                foreach (var kvp in conditions)
                {
                    var condition = Expression.IfThen(
                        ExpressionEx.Not(kvp.Key.Apply(source, destination)),
                        Expression.Block(kvp.Value));
                    lines.Add(condition);
                }
            }

            return lines.Count > 0 ? (Expression)Expression.Block(lines) : Expression.Empty();
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

            var classModel = GetClassModel(arg.DestinationType);
            var classConverter = CreateClassConverter(source, classModel, arg);
            var members = classConverter.Members;

            var lines = new List<MemberBinding>();
            if (memberInit != null)
                lines.AddRange(memberInit.Bindings);
            foreach (var member in members)
            {
                var value = CreateAdaptExpression(member.Getter, member.DestinationMember.Type, arg);

                //special null property check for projection
                //if we don't set null to property, EF will create empty object
                //except collection type & complex type which cannot be null
                if (arg.MapType == MapType.Projection
                    && member.Getter.Type != member.DestinationMember.Type
                    && !member.Getter.Type.IsCollection()
                    && !member.DestinationMember.Type.IsCollection()
                    && member.Getter.Type.GetTypeInfo().GetCustomAttributes(true).All(attr => attr.GetType().Name != "ComplexTypeAttribute")
                    && member.Getter.CanBeNull())
                {
                    var compareNull = Expression.Equal(member.Getter, Expression.Constant(null, member.Getter.Type));
                    value = Expression.Condition(
                        compareNull,
                        member.DestinationMember.Type.CreateDefault(),
                        value);
                }
                var bind = Expression.Bind((MemberInfo)member.DestinationMember.Info, value);
                lines.Add(bind);
            }

            return Expression.MemberInit(newInstance, lines);
        }

        private ClassModel GetClassModel(Type destinationType)
        {
            return new ClassModel
            {
                Members = destinationType.GetFieldsAndProperties(allowNoSetter: false, accessorFlags: BindingFlags.NonPublic | BindingFlags.Public)
            };
        }
    }
}
