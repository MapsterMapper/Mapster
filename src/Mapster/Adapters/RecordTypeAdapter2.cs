using Mapster.Models;
using Mapster.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using static Mapster.IgnoreDictionary;

namespace Mapster.Adapters
{
    internal class RecordTypeAdapter2 : ClassAdapter
    {
        private ClassMapping? ClassConverterContext;
        protected override int Score => -149;
        protected override bool UseTargetValue => false;
        protected override bool CanMap(PreCompileArgument arg)
        {
            return arg.DestinationType.IsRecordType();
        }

        protected override bool CanInline(Expression source, Expression? destination, CompileArgument arg)
        {
            if (arg.MapType == MapType.Projection)
                return true;
            return false;
        }

        protected override Expression CreateInlineExpression(Expression source, CompileArgument arg, bool IsRequiredOnly = false)
        {
            return CreateInstantiationExpression(source, arg);
        }
        protected override Expression CreateInstantiationExpression(Expression source, Expression? destination, CompileArgument arg)
        {
            //new TDestination(src.Prop1, src.Prop2)
            Expression installExpr;

            if (arg.GetConstructUsing() != null || arg.Settings.MapToConstructor != null || arg.DestinationType == null)
                installExpr = base.CreateInstantiationExpression(source, destination, arg);
            else
            {
                var ctor = arg.DestinationType.GetConstructors()
                        .OrderByDescending(it => it.GetParameters().Length).ToArray().FirstOrDefault(); // Will be used public constructor with the maximum number of parameters 
                var classModel = GetConstructorModel(ctor, false);
                var restorParamModel = GetSetterModel(arg);
                var classConverter = CreateClassConverter(source, classModel, arg, ctorMapping: true);
                installExpr = CreateInstantiationExpression(source, classConverter, arg, destination, restorParamModel);
            }


            return RecordInlineExpression(source, destination, arg, installExpr); // Activator field when not include in public ctor
        }

        private Expression? RecordInlineExpression(Expression source, Expression? destination, CompileArgument arg, Expression installExpr)
        {
            //new TDestination {
            //  Prop1 = convert(src.Prop1),
            //  Prop2 = convert(src.Prop2),
            //}

            var exp = installExpr;
            var memberInit = exp as MemberInitExpression;
            var newInstance = memberInit?.NewExpression ?? (NewExpression)exp;
            var contructorMembers = newInstance.Constructor?.GetParameters().ToList() ?? new();
            var classModel = GetSetterModel(arg);
            var classConverter = CreateClassConverter(source, classModel, arg, destination: destination, recordRestorMemberModel: classModel);
            var members = classConverter.Members;

            ClassConverterContext = classConverter;

            var lines = new List<MemberBinding>();
            if (memberInit != null)
                lines.AddRange(memberInit.Bindings);
            foreach (var member in members)
            {
                if (!arg.Settings.Resolvers.Any(r => r.DestinationMemberName == member.DestinationMember.Name)
                    && contructorMembers.Any(x => string.Equals(x.Name, member.DestinationMember.Name, StringComparison.InvariantCultureIgnoreCase)))
                    continue;

                if (member.DestinationMember.SetterModifier == AccessModifier.None)
                    continue;
                               
                if (member.Ignore.WithOutCondition && !member.IsMaybeReMapping)
                {
                    if (member.DestinationMember.IsBackField) // not restore Backfields
                        continue;

                    if (arg.MapType == MapType.MapToTarget)
                        lines.Add(
                            Expression.Bind((MemberInfo)member.DestinationMember.Info!, member.DestinationMember.GetExpression(destination)));
                    //else
                    //    lines.Add(
                    //        Expression.Bind((MemberInfo)member.DestinationMember.Info!, member.DestinationMember.Type.CreateDefault()));
                    continue;
                }

                var adapt = GetMemberInlineAdapter(member, arg);

                var bind = Expression.Bind((MemberInfo)member.DestinationMember.Info!, adapt);
                lines.Add(bind);
            }

            return Expression.MemberInit(newInstance, lines);
        }

        protected override Expression CreateBlockExpression(Expression source, Expression result, Expression? destination, CompileArgument arg)
        {
            var classModel = GetSetterModel(arg);
            var classConverter = CreateClassConverter(source, classModel, arg, destination ?? result, result:result, recordRestorMemberModel:classModel);
            var members = classConverter.Members;

            var lines = new List<Expression>();

            foreach (var member in members)
            {
                if (member.UseDestinationValue && member.DestinationMember.SetterModifier == AccessModifier.None)
                {
                    if (arg.MapType == MapType.MapToTarget && member.RestoreDestinationMemberExp != null)
                    {
                        if (member.DestinationMember.IsBackField) // not restore Backfields
                            continue;

                        var adapt = member.RestoreDestinationMemberExp
                            .ApplyUseDesitationValueAndInitOnlyProps(member, arg);

                        if (adapt == null)
                            continue;

                        lines.Add(adapt);
                    }
                    if (member.Ignore.WithOutCondition && !member.IsMaybeReMapping)
                        continue;
                        
                    lines.AddRange(GetMemberMapToTargetAdapter(member, destination, result, arg));
                }
            }

            return lines.Count > 0 ? (Expression)Expression.Block(lines) : Expression.Empty();
        }
    }
}
