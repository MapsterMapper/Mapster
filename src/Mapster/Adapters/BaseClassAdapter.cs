using Mapster.Models;
using Mapster.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Mapster.Adapters
{
    internal abstract class BaseClassAdapter : BaseAdapter
    {
        protected override ObjectType ObjectType => ObjectType.Class;
        protected override bool UseTargetValue => true;

        #region Build the Adapter Model

        protected ClassMapping CreateClassConverter(Expression source, ClassModel classModel, CompileArgument arg, Expression? destination = null, Expression? result = null, bool ctorMapping = false, ClassModel recordRestorMemberModel = null)
        {
            var destinationMembers = classModel.Members;
            var unmappedDestinationMembers = new List<string>();
            var properties = new List<MemberMapping>();
            arg.ConstructorMapping = ctorMapping;

            if (arg.Settings.IgnoreNonMapped == true)
                IgnoreNonMapped(classModel,arg);
          
            var sources = new List<ResolverSourceInput> {(ResolverSourceInput)source};
            sources.AddRange(
                arg.Settings.ExtraSources.Select(src => src.Activate(source, arg)));
            foreach (var destinationMember in destinationMembers)
            {
                ProcessIgnores(arg, destinationMember, out var ignore);

                // perf optimization. Backfields are not processed in the standard mapping pipeline.
                if (destinationMember.IsBackField && ignore.WithOutCondition) 
                    continue;

                var propertyModel = new MemberMapping();
   
                var resolvers = arg.Settings.ValueAccessingStrategies.AsEnumerable();
                if (arg.Settings.IgnoreNonMapped == true)
                    resolvers = resolvers.Where(ValueAccessingStrategy.CustomResolvers.Contains);
                var resolver = (from fn in resolvers
                        from src in sources
                        select fn(src, destinationMember, propertyModel, arg))
                    .FirstOrDefault(result => result);
                
                var nextIgnore = arg.Settings.Ignore.Next((ParameterExpression)source, (ParameterExpression?)destination, destinationMember.Name);
                var nextResolvers = arg.Settings.Resolvers.Next(arg.Settings.Ignore, (ParameterExpression)source, destinationMember.Name)
                    .ToList();

                propertyModel
                    .DestinationMember = destinationMember;
                propertyModel
                    .Ignore = ignore;
                propertyModel
                    .NextResolvers = nextResolvers;
                propertyModel.
                    NextResolvers = nextResolvers;
                propertyModel.
                    NextIgnore = nextIgnore;
                propertyModel.
                    Source = (ParameterExpression)source; //  must be determined for each GetterLine
                propertyModel.
                    Destination = (ParameterExpression?)destination;
                propertyModel.
                    Result = (ParameterExpression?)result;
                propertyModel.
                    UseDestinationValue = IsCanUsingDestinationValue(arg, destinationMember);


                if(arg.MapType == MapType.ApplyNullPropagation &&
                    propertyModel.isNotFoundGetter  
                    && !arg.DestinationType.IsRecordType()  
                    && destinationMember.Info is PropertyInfo propinfo)
                {
                    if (propinfo.GetCustomAttributes()
                        .Any(y => y.GetType().FullName == "System.Runtime.CompilerServices.RequiredMemberAttribute"))
                    {
                        propertyModel.GetterLines.Add(new GetterLine(source, destinationMember.Type.CreateDefault(arg), null));
                    }
                }

                if (arg.MapType == MapType.MapToTarget && arg.DestinationType.IsRecordType())
                {
                    
                    propertyModel
                        .RestoreDestinationMemberExp = TryRestoreRecordMember(destinationMember, recordRestorMemberModel, destination, arg);

                    if (propertyModel.GetterLines.Count == 0 && propertyModel.RestoreDestinationMemberExp != null)
                        propertyModel.GetterLines.Add(new GetterLine(destination, destinationMember.GetExpression(destination), null));

                }
                if (propertyModel.GetterLines.Count != 0 || propertyModel.Ignore.IsValid)
                {
                    propertyModel.CreateNullPropagationChecker(arg);
                    properties.Add(propertyModel);
                }
                else
                {
                    if (arg.Settings.IgnoreNonMapped != true &&
                        arg.Settings.Unflattening == true &&
                        arg.DestinationType.GetDictionaryType() == null &&
                        arg.SourceType.GetDictionaryType() == null)
                    {
                        var extra = ValueAccessingStrategy.FindUnflatteningPairs(source, destinationMember, arg)
                            .Next(arg.Settings.Ignore, (ParameterExpression)source, destinationMember.Name);
                        nextResolvers.AddRange(extra);
                    }

                    if (classModel.ConstructorInfo != null)
                    {
                        var info = (ParameterInfo)destinationMember.Info!;
                        if (!info.IsOptional)
                        {
                            if (classModel.BreakOnUnmatched)
                                return null!;
                            if(!arg.Settings.Ignore.Any(x=>x.Key == destinationMember.Name)) // Don't mark a constructor parameter if it was explicitly ignored
                                unmappedDestinationMembers.Add(destinationMember.Name);
                        }

                        properties.Add(propertyModel);
                    }
                    else if (propertyModel.HasSettings())
                    {
                        propertyModel.GetterLines.Add(new GetterLine(source, Expression.New(typeof(Never)), null));
                        properties.Add(propertyModel);
                    }
                    else if (destinationMember.UseDestinationValue(arg) || destinationMember.SetterModifier != AccessModifier.None)
                    {
                        if (classModel.BreakOnUnmatched)
                            return null!;
                        unmappedDestinationMembers.Add(destinationMember.Name);
                    }
                }
            }

            var requireDestMemberSource = arg.Settings.RequireDestinationMemberSource ??
                                          arg.Context.Config.RequireDestinationMemberSource;
            if (requireDestMemberSource &&
                unmappedDestinationMembers.Count > 0 &&
                arg.Settings.SkipDestinationMemberCheck != true)
            {
                throw new InvalidOperationException($"The following members of destination class {arg.DestinationType} do not have a corresponding source member mapped or ignored:{string.Join(",", unmappedDestinationMembers)}");
            }

            return new ClassMapping
            {
                ConstructorInfo = classModel.ConstructorInfo,
                Members = properties,
            };
        }

        protected static bool IsCanUsingDestinationValue(CompileArgument arg, IMemberModelEx destinationMember)
        {
            if (arg.MapType == MapType.Projection)
                return false;
            if (destinationMember.UseDestinationValue(arg) || arg.Settings.UseDestinationMembers.Contains(destinationMember.Name))
                return true;

            return false;
        }

        protected static void ProcessIgnores(
            CompileArgument arg,
            IMemberModel destinationMember, 
            out IgnoreDictionary.IgnoreItem ignore)
        {
            if (!destinationMember.ShouldMapMember(arg, MemberSide.Destination))
                ignore = new IgnoreDictionary.IgnoreItem(); // equal to set Ignore without condition;
            else
            {
                ignore = IgnoreDictionary.IgnoreItem.InvalidIgnore; // Ignore is not set

                if (arg.Settings.Ignore.TryGetValue(destinationMember.Name, out var realIgnore))
                    ignore = realIgnore; // сonditional Ignore set
            }
        }

        protected Expression CreateInstantiationExpression(Expression source, ClassMapping classConverter, CompileArgument arg, Expression? destination, ClassModel recordRestorParamModel = null)
        {
            var members = classConverter.Members;

            var arguments = new List<Expression>();

            foreach (var member in members)
            {
                var parameterInfo = (ParameterInfo)member.DestinationMember.Info!;
                Expression defaultConst;
                Expression getter;

#if NETSTANDARD2_0
                try
                {
                    defaultConst = parameterInfo.IsOptional && parameterInfo.DefaultValue != null
                    ? Expression.Constant(parameterInfo.DefaultValue, member.DestinationMember.Type)
                    : parameterInfo.ParameterType.CreateDefault(arg);
                }
                catch (FormatException)
                {
                    defaultConst = parameterInfo.ParameterType.CreateDefault(arg);
                }

#else
                defaultConst = parameterInfo.IsOptional && parameterInfo.DefaultValue != null
                    ? Expression.Constant(parameterInfo.DefaultValue, member.DestinationMember.Type)
                    : parameterInfo.ParameterType.CreateDefault(arg);
#endif
                
                if (member.isNotFoundGetter)
                {
                    getter = defaultConst;

                    if (arg.MapType == MapType.MapToTarget && arg.DestinationType.IsRecordType())
                        getter = TryRestoreRecordMember(member.DestinationMember,recordRestorParamModel,destination, arg) ?? getter;
                }
                else
                {
                    getter = GetMemberInlineAdapter(member, arg);
                                                           
                    if (member.Ignore.Condition != null)
                    {
                        var body = member.Ignore.IsChildPath
                            ? member.Ignore.Condition.Body
                            : member.Ignore.Condition.Apply(arg.MapType, source, arg.DestinationType.CreateDefault(arg));
                        var condition = ExpressionEx.Not(body);
                        getter = Expression.Condition(condition, getter, defaultConst);
                    }
                    else
                        if (arg.Settings.Ignore.Any(x => x.Key == member.DestinationMember.Name))
                    {
                        getter = defaultConst;

                        if (arg.MapType == MapType.MapToTarget && arg.DestinationType.IsRecordType())
                           getter = TryRestoreRecordMember(member.DestinationMember, recordRestorParamModel, destination, arg) ?? getter;
                    }

                }
                arguments.Add(getter);
            }

            return Expression.New(classConverter.ConstructorInfo!, arguments);
        }

        protected virtual ClassModel GetConstructorModel(ConstructorInfo ctor, bool breakOnUnmatched)
        {
            return new ClassModel
            {
                BreakOnUnmatched = breakOnUnmatched,
                ConstructorInfo = ctor,
                Members = ctor.GetParameters().Select(ReflectionUtils.CreateModel)
            };
        }

        protected virtual ClassModel GetSetterModel(CompileArgument arg)
        {
            return new ClassModel
            {
                Members = arg.DestinationType.GetFieldsAndProperties(arg,true)
            };
        }

        protected void IgnoreNonMapped (ClassModel classModel, CompileArgument arg)
        {
            var notMappingToIgnore = LinqCompat.ExceptBy(
                classModel.Members,
                arg.Settings.Resolvers.Select(x => x.DestinationMemberName),
                y => y.Name);

            foreach (var item in notMappingToIgnore)
            {
                if (!item.ShouldMapMember(arg, MemberSide.Destination))
                    continue;

                arg.Settings.Ignore.TryAdd(item.Name, new IgnoreDictionary.IgnoreItem());
            }
        }

        protected virtual ClassModel GetOnlyRequiredPropertySetterModel(CompileArgument arg)
        {
            return new ClassModel
            {
                Members = arg.DestinationType.GetFieldsAndProperties(arg,true)
                    .Where(x => x.GetType() == typeof(PropertyModel))
                    .Where(y => ((PropertyInfo)y.Info).GetCustomAttributes()
                    .Any(y => y.GetType().FullName == "System.Runtime.CompilerServices.RequiredMemberAttribute"))
            };
        }

        protected Expression? TryRestoreRecordMember(IMemberModelEx member, ClassModel? restorRecordModel, Expression? destination, CompileArgument arg)
        {
            if (restorRecordModel != null && destination != null)
            {
                var find = restorRecordModel.Members
                               .Where(x => x.Name == member.Name).FirstOrDefault();

                if (find != null)
                {
                    var compareNull = Expression.Equal(destination, Expression.Constant(null, destination.Type));
                    return Expression.Condition(compareNull, member.Type.CreateDefault(arg), Expression.MakeMemberAccess(destination, (MemberInfo)find.Info));
                }

            }

            return null;
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

        /// <summary>
        /// using for Map or Projection cases
        /// </summary>
        /// <param name="member"></param>
        /// <param name="arg"></param>
        /// <returns></returns>
        protected Expression GetMemberInlineAdapter(MemberMapping member, CompileArgument arg)
        {
            Expression? result = null;
            Expression defaultcase = null;

            var modGetterLine = member.GetterLines.All(x => x.Condition == null) 
                ? member.GetterLines.Take(1).Select(x => x.ApplyContextSettings(member, arg)).ToArray()
                : member.GetterLines.Select(x => x.ApplyContextSettings(member, arg)).ToArray();
            var single = modGetterLine.GetIfSingle();

            if (single.getterLine != null && single.modCondition == null)
            {
                member.OverrideSettings = single.getterLine.OverrideSettings; // need refactoring

                if (arg.MapType == MapType.Projection)
                {
                    var test = single.getterLine.NullPropagationChecker.ApplyProjectionNullCheck(single.modgetter);
                    var adapt = CreateAdaptExpression(single.modgetter, member.DestinationMember.Type, arg, member);
                    var def = adapt.Type.CreateDefault();

                    return Expression.Condition(test, adapt, def);
                }    
                  
                return CreateAdaptExpression(single.modgetter, member.DestinationMember.Type, arg, member);
            }
               
            for (var i = 0; i < modGetterLine.Length; i++)
            {
                member.OverrideSettings = modGetterLine[i].getterLine.OverrideSettings; // need refactoring

                modGetterLine[i].TransformFunc = CreateAdaptExpression(modGetterLine[i].modgetter, member.DestinationMember.Type, arg, member);
            }

            defaultcase = member.RestoreDestinationMemberExp 
                ?? modGetterLine.Where(x => x.modCondition == null).FirstOrDefault().TransformFunc ?? member.DestinationMember.Type.CreateDefault();

            foreach (var item in modGetterLine.Where(x => x.modCondition != null).Reverse())
            {
                if (result == null)
                {
                    var transformDefault = item.TransformFunc.Type == defaultcase.Type
                        ? defaultcase : item.TransformFunc.Type.CreateDefault();

                    result = Expression.Condition(item.modCondition, item.TransformFunc, transformDefault);
                }
                else
                    result = Expression.Condition(item.modCondition, item.TransformFunc, result);
            }

            return result;
        }


        protected List<Expression> GetMemberMapToTargetAdapter(MemberMapping member, Expression? destination, Expression result, CompileArgument arg)
        {
            var resultLines = new List<Expression>();

            var resultMember = arg.MapType == MapType.MapToTarget || member.UseDestinationValue
                    ? member.DestinationMember.GetExpression(result)
                    : null;

            var modGetterLine = member.GetterLines.All(x => x.Condition == null)
               ? member.GetterLines.Take(1).Select(x => x.ApplyContextSettings(member,arg))
               : member.GetterLines.Select(x => x.ApplyContextSettings(member, arg));

            var single = modGetterLine.GetIfSingle();

            if (single.getterLine != null && single.modCondition == null)

            {
                member.OverrideSettings = single.getterLine.OverrideSettings; // need refactoring

                var adapt = CreateAdaptExpression(single.modgetter, member.DestinationMember.Type, arg, member, resultMember);
                var transformAdapt = adapt.ApplyUseDesitationValueAndInitOnlyProps(member, arg);

                if(adapt == transformAdapt && member.DestinationMember.SetterModifier != AccessModifier.None)
                    resultLines.Add(member.DestinationMember.SetExpression(result, adapt));
                else
                    resultLines.Add(transformAdapt);
            }


            foreach (var item in modGetterLine.Where(x => x.modCondition != null))
            {
                if (member.DestinationMember.SetterModifier == AccessModifier.None
                    && !member.UseDestinationValue)
                    continue;

                member.OverrideSettings = item.getterLine.OverrideSettings; // need refactoring

                var adapt = CreateAdaptExpression(item.modgetter, member.DestinationMember.Type, arg, member, resultMember);
                var transformAdapt = adapt.ApplyUseDesitationValueAndInitOnlyProps(member, arg);

                if (transformAdapt == null)
                    continue;

                if (adapt == transformAdapt && member.DestinationMember.SetterModifier != AccessModifier.None)
                {
                    resultLines.Add(
                    Expression.IfThen(item.modCondition, member.DestinationMember.SetExpression(result, adapt)));
                }
                else
                    resultLines.Add(
                        Expression.IfThen(item.modCondition, transformAdapt));
            }


            if(member.Ignore.Condition != null)
            {
                var test = member.Ignore.IsChildPath
                            ? member.Ignore.Condition.Body
                            : member.Ignore.Condition.Apply(arg.MapType, member.Source, destination);

                var ApplyIgnore = Expression.IfThen(Expression.Not(test), Expression.Block(resultLines));
                resultLines.Clear();
                resultLines.Add(ApplyIgnore);

            }

            return resultLines;
        }

#endregion
    }
}
