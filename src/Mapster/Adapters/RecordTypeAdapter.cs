using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Mapster.Utils;

namespace Mapster.Adapters
{
    internal class RecordTypeAdapter : ClassAdapter
    {
        protected override int Score => -149;
        protected override bool UseTargetValue => false;

        protected override bool CanMap(PreCompileArgument arg)
        {
            return arg.DestinationType.IsRecordType();
        }

        protected override Expression CreateInstantiationExpression(Expression source, Expression? destination, CompileArgument arg)
        {
            //new TDestination(src.Prop1, src.Prop2)

            if (arg.GetConstructUsing() != null)
                return base.CreateInstantiationExpression(source, destination, arg);

            var destType = arg.DestinationType.GetTypeInfo().IsInterface
                ? DynamicTypeGenerator.GetTypeForInterface(arg.DestinationType, arg.Settings.Includes.Count > 0)
                : arg.DestinationType;
            if (destType == null)
                return base.CreateInstantiationExpression(source, destination, arg);
            var ctor = destType.GetConstructors()
                    .OrderByDescending(it => it.GetParameters().Length).ToArray().FirstOrDefault(); // Will be used public constructor with the maximum number of parameters 
            var classModel = GetConstructorModel(ctor, false);
            var classConverter = CreateClassConverter(source, classModel, arg, ctorMapping:true);
            var installExpr = CreateInstantiationExpression(source, classConverter, arg, destination);
            return RecordInlineExpression(source, arg, installExpr); // Activator field when not include in public ctor
        }

        protected override Expression CreateBlockExpression(Expression source, Expression destination, CompileArgument arg)
        {
            return Expression.Empty();
        }

        protected override Expression CreateInlineExpression(Expression source, CompileArgument arg)
        {
            return base.CreateInstantiationExpression(source, arg);
        }

        private Expression? RecordInlineExpression(Expression source, CompileArgument arg, Expression installExpr)
        {
            //new TDestination {
            //  Prop1 = convert(src.Prop1),
            //  Prop2 = convert(src.Prop2),
            //}

            var exp = installExpr;
            var memberInit = exp as MemberInitExpression;
            var newInstance = memberInit?.NewExpression ?? (NewExpression)exp;
            var contructorMembers = newInstance.Arguments.OfType<MemberExpression>().Select(me => me.Member).ToArray();
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

                if (!arg.Settings.Resolvers.Any(r => r.DestinationMemberName == member.DestinationMember.Name)
                    && member.Getter is MemberExpression memberExp && contructorMembers.Contains(memberExp.Member))
                    continue;

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
