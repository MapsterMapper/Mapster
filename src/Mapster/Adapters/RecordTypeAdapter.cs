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
    internal class RecordTypeAdapter : ClassAdapter
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
            return false;
        }

        protected override Expression CreateInlineExpression(Expression source, CompileArgument arg)
        {
            return base.CreateInstantiationExpression(source, arg);
        }
        protected override Expression CreateInstantiationExpression(Expression source, Expression? destination, CompileArgument arg)
        {
            //new TDestination(src.Prop1, src.Prop2)

            Expression installExpr;

            if (arg.GetConstructUsing() != null || arg.DestinationType == null)
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

                var adapt = CreateAdaptExpression(member.Getter, member.DestinationMember.Type, arg, member);

                if (arg.MapType == MapType.MapToTarget && arg.Settings.IgnoreNullValues == true && member.Getter.CanBeNull()) // add IgnoreNullValues support
                {
                    if (adapt is ConditionalExpression condEx)
                    {
                        if (condEx.Test is BinaryExpression { NodeType: ExpressionType.Equal } binEx &&
                            binEx.Left == member.Getter &&
                            binEx.Right is ConstantExpression { Value: null })
                            adapt = condEx.IfFalse;
                    }
                    var destinationCompareNull = Expression.Equal(destination, Expression.Constant(null, destination.Type));
                    var sourceCondition = Expression.NotEqual(member.Getter, Expression.Constant(null, member.Getter.Type));
                    var destinationCanbeNull = Expression.Condition(destinationCompareNull, member.DestinationMember.Type.CreateDefault(), member.DestinationMember.GetExpression(destination));
                    adapt = Expression.Condition(sourceCondition, adapt, destinationCanbeNull);
                }




                //special null property check for projection
                //if we don't set null to property, EF will create empty object
                //except collection type & complex type which cannot be null
                if (arg.MapType == MapType.Projection
                    && member.Getter.Type != member.DestinationMember.Type
                    && !member.Getter.Type.IsCollection()
                    && !member.DestinationMember.Type.IsCollection()
                    && member.Getter.Type.GetTypeInfo().GetCustomAttributesData().All(attr => attr.GetAttributeType().Name != "ComplexTypeAttribute"))
                {
                    adapt = member.Getter.NotNullReturn(adapt);
                }
                var bind = Expression.Bind((MemberInfo)member.DestinationMember.Info!, adapt);
                lines.Add(bind);
            }

            if (arg.MapType == MapType.MapToTarget)
                lines.AddRange(RecordIngnoredWithoutConditonRestore(destination, arg, contructorMembers, classModel));

            return Expression.MemberInit(newInstance, lines);
        }

        private List<MemberBinding> RecordIngnoredWithoutConditonRestore(Expression? destination, CompileArgument arg, List<ParameterInfo> contructorMembers, ClassModel restorPropertyModel)
        {
            var members = restorPropertyModel.Members
                             .Where(x => arg.Settings.Ignore.Any(y => y.Key == x.Name));

            var lines = new List<MemberBinding>();


            foreach (var member in members)
            {
                if (destination == null)
                    continue;

                IgnoreItem ignore;
                ProcessIgnores(arg, member, out ignore);

                if (member.SetterModifier == AccessModifier.None ||
                   ignore.Condition != null ||
                   contructorMembers.Any(x => string.Equals(x.Name, member.Name, StringComparison.InvariantCultureIgnoreCase)))
                    continue;

                lines.Add(Expression.Bind((MemberInfo)member.Info, Expression.MakeMemberAccess(destination, (MemberInfo)member.Info)));
            }

            return lines;
        }

        protected override Expression CreateBlockExpression(Expression source, Expression destination, CompileArgument arg)
        {
            // Mapping property Without setter when UseDestinationValue == true

            var result = destination;
            var classModel = GetSetterModel(arg);
            var classConverter = CreateClassConverter(source, classModel, arg, result);
            var members = classConverter.Members;

            var lines = new List<Expression>();

            foreach (var member in members)
            {
                if (member.DestinationMember.SetterModifier == AccessModifier.None && member.UseDestinationValue)
                {

                    if (member.DestinationMember is PropertyModel && member.DestinationMember.Type.IsValueType 
                        || member.DestinationMember.Type.IsMapsterPrimitive()
                        || member.DestinationMember.Type.IsRecordType())
                    {
                        
                        Expression adapt;
                        if (member.DestinationMember.Type.IsRecordType())
                            adapt = arg.Context.Config.CreateMapInvokeExpressionBody(member.Getter.Type, member.DestinationMember.Type, member.Getter);
                        else
                            adapt = CreateAdaptExpression(member.Getter, member.DestinationMember.Type, arg, member, result);

                        var blocks = Expression.Block(SetValueTypeAutoPropertyByReflection(member, adapt, classModel));
                        var lambda = Expression.Lambda(blocks, parameters: new[] { (ParameterExpression)source, (ParameterExpression)destination });

                        if (arg.Settings.IgnoreNullValues == true && member.Getter.CanBeNull())
                        {

                            if (arg.MapType != MapType.MapToTarget)
                            {
                                var condition = Expression.NotEqual(member.Getter, Expression.Constant(null, member.Getter.Type));
                                lines.Add(Expression.IfThen(condition, Expression.Invoke(lambda, source, destination)));
                                continue;
                            }

                            if (arg.MapType == MapType.MapToTarget)
                            {
                                var var2Param = ClassConverterContext.Members.Where(x => x.DestinationMember.Name == member.DestinationMember.Name).FirstOrDefault();

                                Expression destMemberVar2 = var2Param.DestinationMember.GetExpression(var2Param.Destination);
                                var ParamLambdaVar2 = destMemberVar2;
                                if(member.DestinationMember.Type.IsRecordType())
                                    ParamLambdaVar2 = arg.Context.Config.CreateMapInvokeExpressionBody(member.Getter.Type, member.DestinationMember.Type, destMemberVar2);
                                
                                var blocksVar2 = Expression.Block(SetValueTypeAutoPropertyByReflection(member, ParamLambdaVar2, classModel));
                                var lambdaVar2 = Expression.Lambda(blocksVar2, parameters: new[] { (ParameterExpression)var2Param.Destination, (ParameterExpression)destination });
                                var adaptVar2 = Expression.Invoke(lambdaVar2, var2Param.Destination, destination);


                                Expression conditionVar2;
                                if (destMemberVar2.CanBeNull())
                                {
                                    var complexcheck = Expression.AndAlso(Expression.NotEqual(var2Param.Destination, Expression.Constant(null, var2Param.Destination.Type)), // if(var2 != null && var2.Prop != null)
                                    Expression.NotEqual(destMemberVar2, Expression.Constant(null, var2Param.Getter.Type)));
                                    conditionVar2 = Expression.IfThen(complexcheck, adaptVar2);
                                }
                                else
                                    conditionVar2 = Expression.IfThen(Expression.NotEqual(var2Param.Destination, Expression.Constant(null, var2Param.Destination.Type)), adaptVar2);

                                var condition = Expression.NotEqual(member.Getter, Expression.Constant(null, member.Getter.Type));
                                lines.Add(Expression.IfThenElse(condition, Expression.Invoke(lambda, source, destination), conditionVar2));
                                continue;
                            }
                        }

                        lines.Add(Expression.Invoke(lambda, source, destination));
                    }
                    else
                    {
                        var destMember = member.DestinationMember.GetExpression(destination);
                        var adapt = CreateAdaptExpression(member.Getter, member.DestinationMember.Type, arg, member, destMember);

                        if (arg.Settings.IgnoreNullValues == true && member.Getter.CanBeNull())
                        {
                            if (arg.MapType != MapType.MapToTarget)
                            {
                                var condition = Expression.NotEqual(member.Getter, Expression.Constant(null, member.Getter.Type));
                                lines.Add(Expression.IfThen(condition, adapt));
                                continue;
                            }
                            if (arg.MapType == MapType.MapToTarget)
                            {
                                var var2Param = ClassConverterContext.Members.Where(x => x.DestinationMember.Name == member.DestinationMember.Name).FirstOrDefault();

                                var destMemberVar2 = var2Param.DestinationMember.GetExpression(var2Param.Destination);
                                var adaptVar2 = CreateAdaptExpression(destMemberVar2, member.DestinationMember.Type, arg, var2Param, destMember);

                                var complexcheck = Expression.AndAlso(Expression.NotEqual(var2Param.Destination, Expression.Constant(null, var2Param.Destination.Type)), // if(var2 != null && var2.Prop != null)
                                    Expression.NotEqual(destMemberVar2, Expression.Constant(null, var2Param.Getter.Type)));
                                var conditionVar2 = Expression.IfThen(complexcheck, adaptVar2);

                                var condition = Expression.NotEqual(member.Getter, Expression.Constant(null, member.Getter.Type));
                                lines.Add(Expression.IfThenElse(condition, adapt, conditionVar2));
                                continue;
                            }


                        }

                        lines.Add(adapt);
                    }

                }
            }

            return lines.Count > 0 ? (Expression)Expression.Block(lines) : Expression.Empty();
        }

        protected static Expression SetValueTypeAutoPropertyByReflection(MemberMapping member, Expression adapt, ClassModel checkmodel)
        {
            var modDesinationMemeberName = $"<{member.DestinationMember.Name}>k__BackingField";
            if (checkmodel.Members.Any(x => x.Name == modDesinationMemeberName) == false) // Property is not autoproperty
                return Expression.Empty();
            var typeofExpression = Expression.Constant(member.Destination!.Type);
            var getPropertyMethod = typeof(Type).GetMethod("GetField", new[] { typeof(string), typeof(BindingFlags) })!;
            var getPropertyExpression = Expression.Call(typeofExpression, getPropertyMethod,
                Expression.Constant(modDesinationMemeberName), Expression.Constant(BindingFlags.Instance | BindingFlags.NonPublic));
            var setValueMethod =
                typeof(FieldInfo).GetMethod("SetValue", new[] { typeof(object), typeof(object) })!;
            var memberAsObject = adapt.To(typeof(object));
            return Expression.Call(getPropertyExpression, setValueMethod,
                new[] { member.Destination, memberAsObject });
        }
    }

}
