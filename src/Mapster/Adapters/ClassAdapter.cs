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

        protected override bool CanInline(Expression source, Expression? destination, CompileArgument arg)
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

            //Ignore, IgnoreNullValue isn't supported by projection
            if (arg.MapType == MapType.Projection)
                return true;
            if (arg.Settings.IgnoreNullValues == true)
                return false;
            if (arg.Settings.Ignore.Any(item => item.Value.Condition != null))
                return false;
            return true;
        }

        protected override Expression CreateInstantiationExpression(Expression source, Expression? destination, CompileArgument arg)
        {
            //new TDestination(src.Prop1, src.Prop2)

            if (arg.GetConstructUsing() != null || arg.Settings.MapToConstructor == null)
                return base.CreateInstantiationExpression(source, destination, arg);

            ClassMapping? classConverter;
            var ctor = arg.Settings.MapToConstructor as ConstructorInfo;
            if (ctor == null)
            {
                var destType = arg.DestinationType.GetTypeInfo().IsInterface
                    ? DynamicTypeGenerator.GetTypeForInterface(arg.DestinationType, arg.Settings.Includes.Count > 0)
                    : arg.DestinationType;
                if (destType == null)
                    return base.CreateInstantiationExpression(source, destination, arg);
                classConverter = destType.GetConstructors()
                    .OrderByDescending(it => it.GetParameters().Length)
                    .Select(it => GetConstructorModel(it, true))
                    .Select(it => CreateClassConverter(source, it, arg))
                    .FirstOrDefault(it => it != null);
            }
            else
            {
                var model = GetConstructorModel(ctor, false);
                classConverter = CreateClassConverter(source, model, arg);
            }

            if (classConverter == null)
                return base.CreateInstantiationExpression(source, destination, arg);

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

            var classModel = GetSetterModel(arg);
            var classConverter = CreateClassConverter(source, classModel, arg, destination);
            var members = classConverter.Members;

            var lines = new List<Expression>();
            Dictionary<LambdaExpression, Tuple<List<Expression>, Expression>>? conditions = null;
            foreach (var member in members)
            {
                if (!member.UseDestinationValue && member.DestinationMember.SetterModifier == AccessModifier.None)
                    continue;

                var destMember = arg.MapType == MapType.MapToTarget || member.UseDestinationValue
                    ? member.DestinationMember.GetExpression(destination)
                    : null;

                var adapt = CreateAdaptExpression(member.Getter, member.DestinationMember.Type, arg, member, destMember);
                if (!member.UseDestinationValue)
                {
                    if (arg.Settings.IgnoreNullValues == true && member.Getter.CanBeNull())
                    {
                        if (adapt is ConditionalExpression condEx)
                        {
                            if (condEx.Test is BinaryExpression {NodeType: ExpressionType.Equal} binEx && 
                                binEx.Left == member.Getter && 
                                binEx.Right is ConstantExpression {Value: null})
                                adapt = condEx.IfFalse;
                        }
                        adapt = member.DestinationMember.SetExpression(destination, adapt);
                        var condition = Expression.NotEqual(member.Getter, Expression.Constant(null, member.Getter.Type));
                        adapt = Expression.IfThen(condition, adapt);
                    }
                    else
                    {
                        adapt = member.DestinationMember.SetExpression(destination, adapt);
                    }
                }
                else if (!adapt.IsComplex())
                    continue;

                if (member.Ignore.Condition != null)
                {
                    conditions ??= new Dictionary<LambdaExpression, Tuple<List<Expression>, Expression>>();
                    if (!conditions.TryGetValue(member.Ignore.Condition, out var tuple))
                    {
                        var body = member.Ignore.IsChildPath
                            ? member.Ignore.Condition.Body
                            : member.Ignore.Condition.Apply(arg.MapType, source, destination);
                        tuple = Tuple.Create(new List<Expression>(), body);
                        conditions[member.Ignore.Condition] = tuple;
                    }
                    tuple.Item1.Add(adapt);
                }
                else
                    lines.Add(adapt);
            }

            if (conditions != null)
            {
                foreach (var kvp in conditions)
                {
                    var condition = Expression.IfThen(
                        ExpressionEx.Not(kvp.Value.Item2),
                        Expression.Block(kvp.Value.Item1));
                    lines.Add(condition);
                }
            }

            return lines.Count > 0 ? (Expression)Expression.Block(lines) : Expression.Empty();
        }

        protected override Expression? CreateInlineExpression(Expression source, CompileArgument arg)
        {
            //new TDestination {
            //  Prop1 = convert(src.Prop1),
            //  Prop2 = convert(src.Prop2),
            //}

            var exp = CreateInstantiationExpression(source, arg);
            var memberInit = exp as MemberInitExpression;
            var newInstance = memberInit?.NewExpression ?? (NewExpression)exp;

            var classModel = GetSetterModel(arg);
            var classConverter = CreateClassConverter(source, classModel, arg);
            var members = classConverter.Members;

            var lines = new List<MemberBinding>();
            if (memberInit != null)
                lines.AddRange(memberInit.Bindings);
            foreach (var member in members)
            {
                if (member.UseDestinationValue)
                    return null;
                if (member.DestinationMember.SetterModifier == AccessModifier.None)
                    continue;

                var value = CreateAdaptExpression(member.Getter, member.DestinationMember.Type, arg, member);

                //special null property check for projection
                //if we don't set null to property, EF will create empty object
                //except collection type & complex type which cannot be null
                if (arg.MapType == MapType.Projection
                    && member.Getter.Type != member.DestinationMember.Type
                    && !member.Getter.Type.IsCollection()
                    && !member.DestinationMember.Type.IsCollection()
                    && member.Getter.Type.GetTypeInfo().GetCustomAttributesData().All(attr => attr.GetAttributeType().Name != "ComplexTypeAttribute"))
                {
                    value = member.Getter.NotNullReturn(value);
                }
                var bind = Expression.Bind((MemberInfo)member.DestinationMember.Info!, value);
                lines.Add(bind);
            }

            return Expression.MemberInit(newInstance, lines);
        }
    }
}
