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

        protected ClassMapping CreateClassConverter(Expression source, ClassModel classModel, CompileArgument arg, Expression? destination = null, bool ctorMapping = false, ClassModel recordRestorMemberModel = null)
        {
            var destinationMembers = classModel.Members;
            var unmappedDestinationMembers = new List<string>();
            var properties = new List<MemberMapping>();
            arg.ConstructorMapping = ctorMapping;

            if (arg.Settings.IgnoreNonMapped == true)
                IgnoreNonMapped(classModel,arg);
          
            var sources = new List<ResolverSourceInput> {new ResolverSourceInput(source)};
            sources.AddRange(
                arg.Settings.ExtraSources.Select(src => ResolverSourceInput.ConvertFrom(src,source,arg)));
            foreach (var destinationMember in destinationMembers)
            {
                if (!destinationMember.ShouldMapMember(arg, MemberSide.Destination))
                    continue;

                var resolvers = arg.Settings.ValueAccessingStrategies.AsEnumerable();
                if (arg.Settings.IgnoreNonMapped == true)
                    resolvers = resolvers.Where(ValueAccessingStrategy.CustomResolvers.Contains);
                var resolver = (from fn in resolvers
                        from src in sources
                        select fn(src, destinationMember, arg))
                    .FirstOrDefault(result => result != null);
                var getter = resolver?.Exp;
                var overideSettings = resolver?.Settings;

                if (ProcessIgnores(arg, destinationMember,out var ignore, resolver) && !ctorMapping)
                    continue;

                // ReadyToCleanUp
                // source in overideSettings is not source in this context 
                //  if (overideSettings != null && getter != null)
                //  getter = ReplaceOvverideExpressionParam.Replace(getter, source);

                if (arg.MapType == MapType.Projection && getter != null)
                {
                    var s = new TopLevelMemberNameVisitor();

                    s.Visit(getter);

                    if (s.MemberName != null && arg.Settings.ProjectToTypeResolvers.TryGetValue(s.MemberName, out var match))
                    {
                        arg.Settings.Resolvers.Add(new InvokerModel
                        {
                            Condition = null,
                            DestinationMemberName = destinationMember.Name,
                            Invoker = (LambdaExpression)match.Operand,
                            SourceMemberName = null,
                            IsChildPath = false

                        });
                    }

                    getter = (from fn in resolvers
                              from src in sources
                              select fn(src, destinationMember, arg))
                    .FirstOrDefault(result => result != null)?.Exp;
                }


                if (arg.MapType == MapType.Projection)
                {

                    var checkgetter = (from fn in resolvers.Where(ValueAccessingStrategy.CustomResolvers.Contains)
                                       from src in sources
                                       select fn(src, destinationMember, arg))
                                       .FirstOrDefault(result => result != null);

                    if (checkgetter == null)
                    {
                        Type destinationType;

                        if (destinationMember.Type.IsNullable())
                            destinationType = destinationMember.Type.GetGenericArguments()[0];
                        else
                            destinationType = destinationMember.Type;

                        if (arg.Settings.ProjectToTypeMapConfig == Enums.ProjectToTypeAutoMapping.OnlyPrimitiveTypes
                            && destinationType.IsMapsterPrimitive() == false)
                            continue;

                        if (arg.Settings.ProjectToTypeMapConfig == Enums.ProjectToTypeAutoMapping.WithoutCollections
                            && destinationType.IsCollectionCompatible() == true)
                            continue;
                    }

                }

                var nextIgnore = arg.Settings.Ignore.Next((ParameterExpression)source, (ParameterExpression?)destination, destinationMember.Name);
                var nextResolvers = arg.Settings.Resolvers.Next(arg.Settings.Ignore, (ParameterExpression)source, destinationMember.Name)
                    .ToList();

                var propertyModel = new MemberMapping
                {
                    DestinationMember = destinationMember,
                    Ignore = ignore,
                    NextResolvers = nextResolvers,
                    NextIgnore = nextIgnore,
                    Source = (ParameterExpression)source,
                    Destination = (ParameterExpression?)destination,
                    UseDestinationValue = IsCanUsingDestinationValue(arg, destinationMember),
                    OverrideSettings = overideSettings
                };
                if(arg.MapType == MapType.ApplyNullPropagation &&
                    getter == null && !arg.DestinationType.IsRecordType()  
                    && destinationMember.Info is PropertyInfo propinfo)
                {
                    if (propinfo.GetCustomAttributes()
                        .Any(y => y.GetType().FullName == "System.Runtime.CompilerServices.RequiredMemberAttribute"))
                    {
                        getter = destinationMember.Type.CreateDefault(arg);
                    }
                }

                if (arg.MapType == MapType.MapToTarget && getter == null && arg.DestinationType.IsRecordType())
                {
                    getter = TryRestoreRecordMember(destinationMember, recordRestorMemberModel, destination, arg) ?? getter;
                }
                if (getter != null)
                {
                    propertyModel.Getter = arg.MapType == MapType.Projection || ctorMapping
                        ? getter
                        : getter.ApplyPropertyNullPropagation(arg, source);
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
                        propertyModel.Getter = Expression.New(typeof(Never));
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

        protected static bool ProcessIgnores(
            CompileArgument arg,
            IMemberModel destinationMember, 
            out IgnoreDictionary.IgnoreItem ignore,
            ResolverResult? resolver = null)
        {
            ignore = new IgnoreDictionary.IgnoreItem();

            if (resolver?.Settings != null)
            {
               if(resolver.Settings.ReMapExtraSource.GetValueOrDefault() 
                    || resolver.Settings.ReMapDestination.Contains(destinationMember.Name)
                    || arg.Settings.ReMapDestinationMembers.Contains(destinationMember.Name))
                    return false;
            }
                
            if (!destinationMember.ShouldMapMember(arg, MemberSide.Destination))
                return true;

            return arg.Settings.Ignore.TryGetValue(destinationMember.Name, out ignore)
                   && ignore.Condition == null;
        }

        protected Expression CreateInstantiationExpression(Expression source, ClassMapping classConverter, CompileArgument arg, Expression? destination, ClassModel recordRestorParamModel = null)
        {
            var members = classConverter.Members;

            var arguments = new List<Expression>();
            // ReadyToCleanUp
            // arg.Context.NullChecks.UnionWith(members.Where(x => x.Getter != null).Select(x => (x.Getter, arg)));
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
                
                if (member.Getter == null)
                {
                    getter = defaultConst;

                    if (arg.MapType == MapType.MapToTarget && arg.DestinationType.IsRecordType())
                        getter = TryRestoreRecordMember(member.DestinationMember,recordRestorParamModel,destination, arg) ?? getter;
                }
                else
                {

                    if (member.Getter.CanBeNull() && member.Ignore.Condition == null
                        && (member.DestinationMember.Type.IsAbstractOrNotPublicCtor()
                            || member.DestinationMember.Type.UnwrapNullable().IsRecordType()))
                    {
                        var compareNull = Expression.Equal(member.Getter, Expression.Constant(null, member.Getter.Type));
                        getter = Expression.Condition(ExpressionEx.Not(compareNull),
                            CreateAdaptExpressionCore(member.Getter, member.DestinationMember.Type, arg, member),
                           defaultConst);
                    }
                    else
                       getter = member.Getter
                            .ApplyNullPropagationFromCtor(CreateAdaptExpressionCore(member.Getter, member.DestinationMember.Type, arg, member,mapTypeCtor:MapType.CtorParam), arg, member);

                    

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
                Members = arg.DestinationType.GetFieldsAndProperties(true)
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
                Members = arg.DestinationType.GetFieldsAndProperties(true)
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

#endregion
    }
}
