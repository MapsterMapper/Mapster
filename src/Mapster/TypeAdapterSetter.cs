using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Mapster.Adapters;
using Mapster.Models;
using Mapster.Utils;

namespace Mapster
{
    public class TypeAdapterSetter
    {
        public readonly TypeAdapterSettings Settings;
        public readonly TypeAdapterConfig Config;
        public TypeAdapterSetter(TypeAdapterSettings settings, TypeAdapterConfig config)
        {
            this.Settings = settings;
            this.Config = config;
        }
    }
    public static class TypeAdapterSetterExtensions
    {
        internal static void CheckCompiled<TSetter>(this TSetter setter) where TSetter : TypeAdapterSetter
        {
            if (setter.Settings.Compiled)
                throw new InvalidOperationException("TypeAdapter.Adapt was already called, please clone or create new TypeAdapterConfig.");
        }

        public static TSetter AddDestinationTransform<TSetter, TDestinationMember>(this TSetter setter, Expression<Func<TDestinationMember, TDestinationMember>> transform) where TSetter : TypeAdapterSetter
        {
            setter.CheckCompiled();

            setter.Settings.DestinationTransforms.Add(new DestinationTransform
            {
                Condition = t => t == typeof(TDestinationMember),
                TransformFunc = _ => transform,
            });
            return setter;
        }

        public static TSetter AddDestinationTransform<TSetter>(this TSetter setter, DestinationTransform transform) where TSetter : TypeAdapterSetter
        {
            setter.CheckCompiled();

            setter.Settings.DestinationTransforms.Add(transform);
            return setter;
        }

        public static TSetter Ignore<TSetter>(this TSetter setter, params string[] names) where TSetter : TypeAdapterSetter
        {
            setter.CheckCompiled();

            foreach (var name in names)
            {
                setter.Settings.Ignore[name] = new IgnoreDictionary.IgnoreItem();
            }
            return setter;
        }

        public static TSetter IgnoreAttribute<TSetter>(this TSetter setter, params Type[] types) where TSetter : TypeAdapterSetter
        {
            setter.CheckCompiled();

            foreach (var type in types)
            {
                setter.Settings.ShouldMapMember.Add((member, _) => member.HasCustomAttribute(type) ? (bool?)false : null);
            }
            return setter;
        }

        public static TSetter IncludeAttribute<TSetter>(this TSetter setter, params Type[] types) where TSetter : TypeAdapterSetter
        {
            setter.CheckCompiled();

            foreach (var type in types)
            {
                setter.Settings.ShouldMapMember.Add((member, _) => member.HasCustomAttribute(type) ? (bool?)true : null);
            }
            return setter;
        }

        public static TSetter IgnoreMember<TSetter>(this TSetter setter, Func<IMemberModel, MemberSide, bool> predicate) where TSetter : TypeAdapterSetter
        {
            setter.CheckCompiled();

            setter.Settings.ShouldMapMember.Add((member, side) => predicate(member, side) ? (bool?)false : null);
            return setter;
        }

        public static TSetter IncludeMember<TSetter>(this TSetter setter, Func<IMemberModel, MemberSide, bool> predicate) where TSetter : TypeAdapterSetter
        {
            setter.CheckCompiled();

            setter.Settings.ShouldMapMember.Add((member, side) => predicate(member, side) ? (bool?)true : null);
            return setter;
        }

        public static TSetter ShallowCopyForSameType<TSetter>(this TSetter setter, bool value) where TSetter : TypeAdapterSetter
        {
            setter.CheckCompiled();

            setter.Settings.ShallowCopyForSameType = value;
            return setter;
        }

        public static TSetter EnumMappingStrategy<TSetter>(this TSetter setter, EnumMappingStrategy strategy) where TSetter : TypeAdapterSetter
        {
            setter.CheckCompiled();

            setter.Settings.MapEnumByName = strategy == Mapster.EnumMappingStrategy.ByName;
            return setter;
        }

        public static TSetter IgnoreNullValues<TSetter>(this TSetter setter, bool value) where TSetter : TypeAdapterSetter
        {
            setter.CheckCompiled();

            setter.Settings.IgnoreNullValues = value;
            return setter;
        }

        public static TSetter PreserveReference<TSetter>(this TSetter setter, bool value) where TSetter : TypeAdapterSetter
        {
            setter.CheckCompiled();

            setter.Settings.PreserveReference = value;
            return setter;
        }

        public static TSetter NameMatchingStrategy<TSetter>(this TSetter setter, NameMatchingStrategy value) where TSetter : TypeAdapterSetter
        {
            setter.CheckCompiled();

            setter.Settings.NameMatchingStrategy = value;
            return setter;
        }

        public static TSetter Map<TSetter, TSourceMember>(
            this TSetter setter, string memberName,
            Expression<Func<TSourceMember>> source) where TSetter : TypeAdapterSetter
        {
            setter.CheckCompiled();

            var invoker = Expression.Lambda(source.Body, Expression.Parameter(typeof(object)));
            setter.Settings.Resolvers.Add(new InvokerModel
            {
                DestinationMemberName = memberName,
                Invoker = invoker,
                Condition = null
            });

            return setter;
        }

        public static TSetter Map<TSetter, TSource, TSourceMember>(
            this TSetter setter, string memberName,
            Expression<Func<TSource, TSourceMember>> source) where TSetter : TypeAdapterSetter
        {
            setter.CheckCompiled();

            setter.Settings.Resolvers.Add(new InvokerModel
            {
                DestinationMemberName = memberName,
                SourceMemberName = source.GetMemberPath(noError: true),
                Invoker = source,
                Condition = null
            });

            return setter;
        }

        public static TSetter Map<TSetter>(
            this TSetter setter, string destinationMemberName, string sourceMemberName) where TSetter : TypeAdapterSetter
        {
            setter.CheckCompiled();

            setter.Settings.Resolvers.Add(new InvokerModel
            {
                DestinationMemberName = destinationMemberName,
                SourceMemberName = sourceMemberName,
                Condition = null
            });

            return setter;
        }

        public static TSetter EnableNonPublicMembers<TSetter>(this TSetter setter, bool value) where TSetter : TypeAdapterSetter
        {
            setter.CheckCompiled();

            setter.Settings.EnableNonPublicMembers = value;
            return setter;
        }

        public static TSetter IgnoreNonMapped<TSetter>(this TSetter setter, bool value) where TSetter : TypeAdapterSetter
        {
            setter.CheckCompiled();

            setter.Settings.IgnoreNonMapped = value;
            return setter;
        }

        public static TSetter AvoidInlineMapping<TSetter>(this TSetter setter, bool value) where TSetter : TypeAdapterSetter
        {
            setter.CheckCompiled();

            setter.Settings.AvoidInlineMapping = value;
            return setter;
        }

        public static TSetter RequireDestinationMemberSource<TSetter>(this TSetter setter, bool value) where TSetter : TypeAdapterSetter
        {
            setter.CheckCompiled();

            setter.Settings.RequireDestinationMemberSource = value;
            return setter;
        }

        public static TSetter GetMemberName<TSetter>(this TSetter setter, Func<IMemberModel, string?> func) where TSetter : TypeAdapterSetter
        {
            setter.CheckCompiled();

            setter.Settings.GetMemberNames.Add((member, _) => func(member));
            return setter;
        }

        public static TSetter GetMemberName<TSetter>(this TSetter setter, Func<IMemberModel, MemberSide, string?> func) where TSetter : TypeAdapterSetter
        {
            setter.CheckCompiled();

            setter.Settings.GetMemberNames.Add(func);
            return setter;
        }

        public static TSetter MapToConstructor<TSetter>(this TSetter setter, bool value) where TSetter : TypeAdapterSetter
        {
            setter.CheckCompiled();

            setter.Settings.MapToConstructor = value ? "*" : null;
            return setter;
        }

        public static TSetter MaxDepth<TSetter>(this TSetter setter, int? value) where TSetter : TypeAdapterSetter
        {
            setter.CheckCompiled();

            setter.Settings.MaxDepth = value;
            return setter;
        }

        public static TSetter Unflattening<TSetter>(this TSetter setter, bool value) where TSetter : TypeAdapterSetter
        {
            setter.CheckCompiled();

            setter.Settings.Unflattening = value;
            return setter;
        }

        public static TSetter UseDestinationValue<TSetter>(this TSetter setter, Func<IMemberModel, bool> func) where TSetter : TypeAdapterSetter
        {
            setter.CheckCompiled();

            setter.Settings.UseDestinationValues.Add(func);
            return setter;
        }

        internal static TSetter Include<TSetter>(this TSetter setter, Type sourceType, Type destType) where TSetter : TypeAdapterSetter
        {
            setter.CheckCompiled();

            setter.Config.Rules.LockAdd(new TypeAdapterRule
            {
                Priority = arg =>
                    arg.SourceType == sourceType &&
                    arg.DestinationType == destType ? (int?)100 : null,
                Settings = setter.Settings
            });

            setter.Settings.Includes.Add(new TypeTuple(sourceType, destType));

            return setter;
        }

        public static TSetter ApplyAdaptAttribute<TSetter>(this TSetter setter, BaseAdaptAttribute attr) where TSetter : TypeAdapterSetter
        {
            if (attr.IgnoreAttributes != null)
                setter.IgnoreAttribute(attr.IgnoreAttributes);
            if (attr.IgnoreNoAttributes != null)
            {
                setter.IgnoreMember((member, _) => !member.GetCustomAttributesData()
                    .Select(it => it.GetAttributeType())
                    .Intersect(attr.IgnoreNoAttributes)
                    .Any());
            }
            if (attr.IgnoreNamespaces != null)
            {
                foreach (var ns in attr.IgnoreNamespaces)
                {
                    setter.IgnoreMember((member, _) => member.Type.Namespace?.StartsWith(ns) == true);
                }
            }
            if (attr.MaxDepth > 0)
                setter.MaxDepth(attr.MaxDepth);
            if (attr.GetBooleanSettingValues(nameof(attr.IgnoreNullValues)) != null)
                setter.IgnoreNullValues(attr.IgnoreNullValues);
            if (attr.GetBooleanSettingValues(nameof(attr.MapToConstructor)) != null)
                setter.MapToConstructor(attr.MapToConstructor);
            if (attr.GetBooleanSettingValues(nameof(attr.PreserveReference)) != null)
                setter.PreserveReference(attr.PreserveReference);
            if (attr.GetBooleanSettingValues(nameof(attr.ShallowCopyForSameType)) != null)
                setter.ShallowCopyForSameType(attr.ShallowCopyForSameType);
            if (attr.GetBooleanSettingValues(nameof(attr.RequireDestinationMemberSource)) != null)
                setter.RequireDestinationMemberSource(attr.RequireDestinationMemberSource);
            return setter;
        }
    }

    public class TypeAdapterSetter<TDestination> : TypeAdapterSetter
    {
        internal TypeAdapterSetter(TypeAdapterSettings settings, TypeAdapterConfig parentConfig)
            : base(settings, parentConfig)
        { }

        public TypeAdapterSetter<TDestination> Ignore(params Expression<Func<TDestination, object>>[] members)
        {
            this.CheckCompiled();

            foreach (var member in members)
            {
                Settings.Ignore[member.GetMemberPath()!] = new IgnoreDictionary.IgnoreItem();
            }
            return this;
        }

        public TypeAdapterSetter<TDestination> Map<TDestinationMember, TSourceMember>(
            Expression<Func<TDestination, TDestinationMember>> member,
            Expression<Func<TSourceMember>> source)
        {
            this.CheckCompiled();

            var invoker = Expression.Lambda(source.Body, Expression.Parameter(typeof (object)));
            if (member.IsIdentity())
            {
                Settings.ExtraSources.Add(invoker);
                return this;
            }

            Settings.Resolvers.Add(new InvokerModel
            {
                DestinationMemberName = member.GetMemberPath()!,
                Invoker = invoker,
                Condition = null
            });
            return this;
        }

        public TypeAdapterSetter<TDestination> Map<TDestinationMember>(
            Expression<Func<TDestination, TDestinationMember>> destinationMember,
            string sourceMemberName)
        {
            this.CheckCompiled();

            if (destinationMember.IsIdentity())
            {
                Settings.ExtraSources.Add(sourceMemberName);
                return this;
            }

            Settings.Resolvers.Add(new InvokerModel
            {
                DestinationMemberName = destinationMember.GetMemberPath()!,
                SourceMemberName = sourceMemberName,
                Condition = null
            });

            return this;
        }

        public TypeAdapterSetter<TDestination> ConstructUsing(Expression<Func<TDestination>> constructUsing)
        {
            this.CheckCompiled();

            Settings.ConstructUsingFactory = arg => constructUsing;

            return this;
        }

        public TypeAdapterSetter<TDestination> BeforeMapping(Action<TDestination> action)
        {
            this.CheckCompiled();

            Settings.BeforeMappingFactories.Add(arg =>
            {
                var p1 = Expression.Parameter(arg.SourceType);
                var p2 = Expression.Parameter(arg.DestinationType);
                var actionType = action.GetType();
                var actionExp = Expression.Constant(action, actionType);
                var invoke = Expression.Call(actionExp, "Invoke", null, p2);
                return Expression.Lambda(invoke, p1, p2);
            });
            return this;
        }

        public TypeAdapterSetter<TDestination> AfterMapping(Action<TDestination> action)
        {
            this.CheckCompiled();

            Settings.AfterMappingFactories.Add(arg =>
            {
                var p1 = Expression.Parameter(arg.SourceType);
                var p2 = Expression.Parameter(arg.DestinationType);
                var actionType = action.GetType();
                var actionExp = Expression.Constant(action, actionType);
                var invoke = Expression.Call(actionExp, "Invoke", null, p2);
                return Expression.Lambda(invoke, p1, p2);
            });
            return this;
        }

        public TypeAdapterSetter<TDestination> MapToConstructor(ConstructorInfo ctor)
        {
            this.CheckCompiled();

            if (ctor != null)
            {
                if (!typeof(TDestination).GetTypeInfo().IsAssignableFrom(ctor.DeclaringType!.GetTypeInfo()))
                    throw new ArgumentException("Constructor cannot be assigned to type TDestination", nameof(ctor));

                if (ctor.DeclaringType!.GetTypeInfo().IsAbstract)
                    throw new ArgumentException("Constructor of abstract type cannot be created", nameof(ctor));
            }

            this.Settings.MapToConstructor = ctor;
            return this;
        }
        
        public TypeAdapterSetter<TDestination> AfterMappingInline(Expression<Action<TDestination>> action)
        {
            this.CheckCompiled();

            var lambda = Expression.Lambda(action.Body, 
                Expression.Parameter(typeof(object), "src"),
                action.Parameters[0]);
            Settings.AfterMappingFactories.Add(arg => lambda);
            return this;
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S4136:Method overloads should be grouped together", Justification = "<Pending>")]
    public class TypeAdapterSetter<TSource, TDestination> : TypeAdapterSetter<TDestination>
    {
        internal TypeAdapterSetter(TypeAdapterSettings settings, TypeAdapterConfig parentConfig)
            : base(settings, parentConfig)
        { }

        #region replace for chaining

        public new TypeAdapterSetter<TSource, TDestination> Ignore(params Expression<Func<TDestination, object>>[] members)
        {
            return (TypeAdapterSetter<TSource, TDestination>)base.Ignore(members);
        }

        public new TypeAdapterSetter<TSource, TDestination> Map<TDestinationMember, TSourceMember>(
            Expression<Func<TDestination, TDestinationMember>> member,
            Expression<Func<TSourceMember>> source)
        {
            return (TypeAdapterSetter<TSource, TDestination>)base.Map(member, source);
        }

        public new TypeAdapterSetter<TSource, TDestination> Map<TDestinationMember>(
            Expression<Func<TDestination, TDestinationMember>> destinationMember,
            string sourceMemberName)
        {
            return (TypeAdapterSetter<TSource, TDestination>)base.Map(destinationMember, sourceMemberName);
        }

        public new TypeAdapterSetter<TSource, TDestination> ConstructUsing(Expression<Func<TDestination>> constructUsing)
        {
            return (TypeAdapterSetter<TSource, TDestination>)base.ConstructUsing(constructUsing);
        }

        public new TypeAdapterSetter<TSource, TDestination> BeforeMapping(Action<TDestination> action)
        {
            return (TypeAdapterSetter<TSource, TDestination>)base.BeforeMapping(action);
        }

        public new TypeAdapterSetter<TSource, TDestination> AfterMapping(Action<TDestination> action)
        {
            return (TypeAdapterSetter<TSource, TDestination>)base.AfterMapping(action);
        }

        public new TypeAdapterSetter<TSource, TDestination> MapToConstructor(ConstructorInfo ctor)
        {
            return (TypeAdapterSetter<TSource, TDestination>) base.MapToConstructor(ctor);
        }

        public new TypeAdapterSetter<TSource, TDestination> AfterMappingInline(Expression<Action<TDestination>> action)
        {
            return (TypeAdapterSetter<TSource, TDestination>) base.AfterMappingInline(action);
        }

        #endregion

        public TypeAdapterSetter<TSource, TDestination> IgnoreIf(
            Expression<Func<TSource, TDestination, bool>> condition,
            params Expression<Func<TDestination, object>>[] members)
        {
            this.CheckCompiled();

            foreach (var member in members)
            {
                var name = member.GetMemberPath()!;
                Settings.Ignore.Merge(name, new IgnoreDictionary.IgnoreItem(condition, false));
            }
            return this;
        }

        public TypeAdapterSetter<TSource, TDestination> IgnoreIf(
            Expression<Func<TSource, TDestination, bool>> condition,
            params string[] members)
        {
            this.CheckCompiled();

            foreach (var member in members)
            {
                Settings.Ignore.Merge(member, new IgnoreDictionary.IgnoreItem(condition, false));
            }
            return this;
        }

        public TypeAdapterSetter<TSource, TDestination> Map<TDestinationMember, TSourceMember>(
            Expression<Func<TDestination, TDestinationMember>> member,
            Expression<Func<TSource, TSourceMember>> source, Expression<Func<TSource, bool>>? shouldMap = null)
        {
            this.CheckCompiled();

            var sourceName = source.GetMemberPath(noError: true);
            if (member.IsIdentity())
            {
                Settings.ExtraSources.Add((object?)sourceName ?? source);
                return this;
            }

            Settings.Resolvers.Add(new InvokerModel
            {
                DestinationMemberName = member.GetMemberPath()!,
                SourceMemberName = sourceName,
                Invoker = source,
                Condition = shouldMap
            });
            return this;
        }

        public TypeAdapterSetter<TSource, TDestination> Map<TSourceMember>(
            string memberName,
            Expression<Func<TSource, TSourceMember>> source, Expression<Func<TSource, bool>>? shouldMap = null)
        {
            this.CheckCompiled();

            Settings.Resolvers.Add(new InvokerModel
            {
                DestinationMemberName = memberName,
                SourceMemberName = source.GetMemberPath(noError: true),
                Invoker = source,
                Condition = shouldMap
            });

            return this;
        }

        public TypeAdapterSetter<TSource, TDestination> ConstructUsing(Expression<Func<TSource, TDestination>> constructUsing)
        {
            this.CheckCompiled();

            Settings.ConstructUsingFactory = arg => constructUsing;

            return this;
        }

        public TypeAdapterSetter<TSource, TDestination> MapWith(Expression<Func<TSource, TDestination>> converterFactory, bool applySettings = false)
        {
            this.CheckCompiled();

            if (applySettings)
            {
                var adapter = new DelegateAdapter(converterFactory);
                Settings.ConverterFactory = adapter.CreateAdaptFunc;
                Settings.ConverterToTargetFactory ??= adapter.CreateAdaptToTargetFunc;
            }
            else
            {
                Settings.ConverterFactory = arg => converterFactory;
                if (Settings.ConverterToTargetFactory == null)
                {
                    var dest = Expression.Parameter(typeof(TDestination));
                    Settings.ConverterToTargetFactory = arg => Expression.Lambda(converterFactory.Body, converterFactory.Parameters[0], dest);
                }
            }

            return this;
        }

        public TypeAdapterSetter<TSource, TDestination> MapToTargetWith(Expression<Func<TSource, TDestination, TDestination>> converterFactory, bool applySettings = false)
        {
            this.CheckCompiled();

            if (applySettings)
            {
                var adapter = new DelegateAdapter(converterFactory);
                Settings.ConverterToTargetFactory = adapter.CreateAdaptToTargetFunc;
            }
            else
                Settings.ConverterToTargetFactory = arg => converterFactory;
            return this;
        }

        public TypeAdapterSetter<TSource, TDestination> BeforeMapping(Action<TSource, TDestination> action)
        {
            this.CheckCompiled();

            Settings.BeforeMappingFactories.Add(arg =>
            {
                var p1 = Expression.Parameter(arg.SourceType);
                var p2 = Expression.Parameter(arg.DestinationType);
                var actionType = action.GetType();
                var actionExp = Expression.Constant(action, actionType);
                var invoke = Expression.Call(actionExp, "Invoke", null, p1, p2);
                return Expression.Lambda(invoke, p1, p2);
            });
            return this;
        }

        public TypeAdapterSetter<TSource, TDestination> AfterMapping(Action<TSource, TDestination> action)
        {
            this.CheckCompiled();

            Settings.AfterMappingFactories.Add(arg =>
            {
                var p1 = Expression.Parameter(arg.SourceType);
                var p2 = Expression.Parameter(arg.DestinationType);
                var actionType = action.GetType();
                var actionExp = Expression.Constant(action, actionType);
                var invoke = Expression.Call(actionExp, "Invoke", null, p1, p2);
                return Expression.Lambda(invoke, p1, p2);
            });
            return this;
        }

        public TypeAdapterSetter<TSource, TDestination> BeforeMappingInline(Expression<Action<TSource, TDestination>> action)
        {
            this.CheckCompiled();

            Settings.BeforeMappingFactories.Add(arg => action);
            return this;
        }
        
        public TypeAdapterSetter<TSource, TDestination> AfterMappingInline(Expression<Action<TSource, TDestination>> action)
        {
            this.CheckCompiled();

            Settings.AfterMappingFactories.Add(arg => action);
            return this;
        }
        
        public TypeAdapterSetter<TSource, TDestination> Include<TDerivedSource, TDerivedDestination>()
            where TDerivedSource: class, TSource
            where TDerivedDestination: class, TDestination
        {
            return this.Include(typeof(TDerivedSource), typeof(TDerivedDestination));
        }

        public TypeAdapterSetter<TSource, TDestination> Inherits<TBaseSource, TBaseDestination>()
        {
            this.CheckCompiled();

            Type baseSourceType = typeof(TBaseSource);
            Type baseDestinationType = typeof(TBaseDestination);

            if (!baseSourceType.GetTypeInfo().IsAssignableFrom(typeof(TSource).GetTypeInfo()))
                throw new InvalidCastException("In order to use inherits, TSource must be inherited from TBaseSource.");

            if (!baseDestinationType.GetTypeInfo().IsAssignableFrom(typeof(TDestination).GetTypeInfo()))
                throw new InvalidCastException("In order to use inherits, TDestination must be inherited from TBaseDestination.");

            if (Config.RuleMap.TryGetValue(new TypeTuple(baseSourceType, baseDestinationType), out var rule))
            {
                Settings.Apply(rule.Settings);
            }
            return this;
        }

        public TypeAdapterSetter<TSource, TDestination> Fork(Action<TypeAdapterConfig> action)
        {
            this.CheckCompiled();

            Settings.Fork = action;
            return this;
        }

        public TypeAdapterSetter<TSource, TDestination> GenerateMapper(MapType mapType)
        {
            this.CheckCompiled();

            Settings.GenerateMapper = mapType;
            return this;
        }

        public TwoWaysTypeAdapterSetter<TSource, TDestination> TwoWays()
        {
            return new TwoWaysTypeAdapterSetter<TSource, TDestination>(this.Config);
        }

        public void Compile()
        {
            this.Config.Compile(typeof(TSource), typeof(TDestination));
        }

        public void CompileProjection()
        {
            this.Config.CompileProjection(typeof(TSource), typeof(TDestination));
        }
    }

    public class TwoWaysTypeAdapterSetter<TSource, TDestination>
    {
        public TypeAdapterSetter<TSource, TDestination> SourceToDestinationSetter { get; }
        public TypeAdapterSetter<TDestination, TSource> DestinationToSourceSetter { get; }

        public TwoWaysTypeAdapterSetter(TypeAdapterConfig config)
        {
            SourceToDestinationSetter = config.ForType<TSource, TDestination>();
            DestinationToSourceSetter = config.ForType<TDestination, TSource>();

            DestinationToSourceSetter.Unflattening(true);
            DestinationToSourceSetter.Settings.SkipDestinationMemberCheck = true;
        }

        public TwoWaysTypeAdapterSetter<TSource, TDestination> AddDestinationTransform<TDestinationMember>(Expression<Func<TDestinationMember, TDestinationMember>> transform)
        {
            SourceToDestinationSetter.AddDestinationTransform(transform);
            if (typeof(TSource) != typeof(TDestination))
                DestinationToSourceSetter.AddDestinationTransform(transform);
            return this;
        }

        public TwoWaysTypeAdapterSetter<TSource, TDestination> AddDestinationTransform(DestinationTransform transform)
        {
            SourceToDestinationSetter.AddDestinationTransform(transform);
            if (typeof(TSource) != typeof(TDestination))
                DestinationToSourceSetter.AddDestinationTransform(transform);
            return this;
        }

        public TwoWaysTypeAdapterSetter<TSource, TDestination> Ignore(params string[] names)
        {
            SourceToDestinationSetter.Ignore(names);
            foreach (var name in names)
            {
                DestinationToSourceSetter.IgnoreMember((model, _) => model.Name == name);
            }
            return this;
        }

        public TwoWaysTypeAdapterSetter<TSource, TDestination> IgnoreAttribute(params Type[] types)
        {
            SourceToDestinationSetter.IgnoreAttribute(types);
            DestinationToSourceSetter.IgnoreAttribute(types);
            return this;
        }

        public TwoWaysTypeAdapterSetter<TSource, TDestination> IncludeAttribute(params Type[] types)
        {
            SourceToDestinationSetter.IncludeAttribute(types);
            DestinationToSourceSetter.IncludeAttribute(types);
            return this;
        }

        public TwoWaysTypeAdapterSetter<TSource, TDestination> IgnoreMember(Func<IMemberModel, MemberSide, bool> predicate)
        {
            SourceToDestinationSetter.IgnoreMember(predicate);
            DestinationToSourceSetter.IgnoreMember((model, side) => predicate(model, side == MemberSide.Source ? MemberSide.Destination : MemberSide.Source));
            return this;
        }

        public TwoWaysTypeAdapterSetter<TSource, TDestination> IncludeMember(Func<IMemberModel, MemberSide, bool> predicate)
        {
            SourceToDestinationSetter.IncludeMember(predicate);
            DestinationToSourceSetter.IncludeMember((model, side) => predicate(model, side == MemberSide.Source ? MemberSide.Destination : MemberSide.Source));
            return this;
        }

        public TwoWaysTypeAdapterSetter<TSource, TDestination> ShallowCopyForSameType(bool value)
        {
            SourceToDestinationSetter.ShallowCopyForSameType(value);
            DestinationToSourceSetter.ShallowCopyForSameType(value);
            return this;
        }

        public TwoWaysTypeAdapterSetter<TSource, TDestination> EnumMappingStrategy(EnumMappingStrategy strategy)
        {
            SourceToDestinationSetter.EnumMappingStrategy(strategy);
            DestinationToSourceSetter.EnumMappingStrategy(strategy);
            return this;
        }

        public TwoWaysTypeAdapterSetter<TSource, TDestination> IgnoreNullValues(bool value)
        {
            SourceToDestinationSetter.IgnoreNullValues(value);
            DestinationToSourceSetter.IgnoreNullValues(value);
            return this;
        }

        public TwoWaysTypeAdapterSetter<TSource, TDestination> PreserveReference(bool value)
        {
            SourceToDestinationSetter.PreserveReference(value);
            DestinationToSourceSetter.PreserveReference(value);
            return this;
        }

        public TwoWaysTypeAdapterSetter<TSource, TDestination> NameMatchingStrategy(NameMatchingStrategy value)
        {
            SourceToDestinationSetter.NameMatchingStrategy(value);
            DestinationToSourceSetter.NameMatchingStrategy(new NameMatchingStrategy
            {
                SourceMemberNameConverter = value.DestinationMemberNameConverter,
                DestinationMemberNameConverter = value.SourceMemberNameConverter,
            });
            return this;
        }

        public TwoWaysTypeAdapterSetter<TSource, TDestination> Map<TSourceMember>(
            string memberName,
            Expression<Func<TSource, TSourceMember>> source)
        {
            SourceToDestinationSetter.Map(memberName, source);
            DestinationToSourceSetter.Map(source, memberName);
            return this;
        }

        public TwoWaysTypeAdapterSetter<TSource, TDestination> Map(string destinationMemberName, string sourceMemberName)
        {
            SourceToDestinationSetter.Map(destinationMemberName, sourceMemberName);
            DestinationToSourceSetter.Map(sourceMemberName, destinationMemberName);
            return this;
        }

        public TwoWaysTypeAdapterSetter<TSource, TDestination> EnableNonPublicMembers(bool value)
        {
            SourceToDestinationSetter.EnableNonPublicMembers(value);
            DestinationToSourceSetter.EnableNonPublicMembers(value);
            return this;
        }

        public TwoWaysTypeAdapterSetter<TSource, TDestination> IgnoreNonMapped(bool value)
        {
            SourceToDestinationSetter.IgnoreNonMapped(value);
            DestinationToSourceSetter.IgnoreNonMapped(value);
            return this;
        }

        public TwoWaysTypeAdapterSetter<TSource, TDestination> AvoidInlineMapping(bool value)
        {
            SourceToDestinationSetter.AvoidInlineMapping(value);
            DestinationToSourceSetter.AvoidInlineMapping(value);
            return this;
        }

        public TwoWaysTypeAdapterSetter<TSource, TDestination> GetMemberName(Func<IMemberModel, string?> func)
        {
            SourceToDestinationSetter.GetMemberName(func);
            DestinationToSourceSetter.GetMemberName(func);
            return this;
        }

        public TwoWaysTypeAdapterSetter<TSource, TDestination> GetMemberName(Func<IMemberModel, MemberSide, string?> func)
        {
            SourceToDestinationSetter.GetMemberName(func);
            DestinationToSourceSetter.GetMemberName((model, side) => func(model, side == MemberSide.Source ? MemberSide.Destination : MemberSide.Source));
            return this;
        }

        public TwoWaysTypeAdapterSetter<TSource, TDestination> MapToConstructor(bool value)
        {
            SourceToDestinationSetter.MapToConstructor(value);
            DestinationToSourceSetter.MapToConstructor(value);
            return this;
        }

        public TwoWaysTypeAdapterSetter<TSource, TDestination> Ignore(params Expression<Func<TDestination, object>>[] members)
        {
            foreach (var member in members)
            {
                var path = member.GetMemberPath()!;
                this.Ignore(path);
            }
            return this;
        }

        public TwoWaysTypeAdapterSetter<TSource, TDestination> Map<TDestinationMember>(
            Expression<Func<TDestination, TDestinationMember>> destinationMember,
            string sourceMemberName)
        {
            SourceToDestinationSetter.Map(destinationMember, sourceMemberName);
            DestinationToSourceSetter.Map(sourceMemberName, destinationMember);
            return this;
        }

        public TwoWaysTypeAdapterSetter<TSource, TDestination> Map<TDestinationMember, TSourceMember>(
            Expression<Func<TDestination, TDestinationMember>> member,
            Expression<Func<TSource, TSourceMember>> source)
        {
            SourceToDestinationSetter.Map(member, source);
            DestinationToSourceSetter.Map(source, member);
            return this;
        }

        public TwoWaysTypeAdapterSetter<TSource, TDestination> Include<TDerivedSource, TDerivedDestination>()
            where TDerivedSource : class, TSource
            where TDerivedDestination : class, TDestination
        {
            SourceToDestinationSetter.Include<TDerivedSource, TDerivedDestination>();
            DestinationToSourceSetter.Include<TDerivedDestination, TDerivedSource>();
            return this;
        }

        public TwoWaysTypeAdapterSetter<TSource, TDestination> Inherits<TBaseSource, TBaseDestination>()
        {
            SourceToDestinationSetter.Inherits<TBaseSource, TBaseDestination>();
            DestinationToSourceSetter.Inherits<TBaseDestination, TBaseSource>();
            return this;
        }

        public TwoWaysTypeAdapterSetter<TSource, TDestination> MaxDepth(int value)
        {
            SourceToDestinationSetter.MaxDepth(value);
            DestinationToSourceSetter.MaxDepth(value);
            return this;
        }

        public TwoWaysTypeAdapterSetter<TSource, TDestination> Fork(Action<TypeAdapterConfig> action)
        {
            SourceToDestinationSetter.Fork(action);
            DestinationToSourceSetter.Fork(action);
            return this;
        }

        public TwoWaysTypeAdapterSetter<TSource, TDestination> GenerateMapper(MapType mapType)
        {
            SourceToDestinationSetter.GenerateMapper(mapType);
            DestinationToSourceSetter.GenerateMapper(mapType);
            return this;
        }
    }
}
