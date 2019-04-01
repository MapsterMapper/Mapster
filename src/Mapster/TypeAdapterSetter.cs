using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Mapster.Adapters;
using Mapster.Models;

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

            setter.Settings.DestinationTransforms.Upsert(transform);
            return setter;
        }

        public static TSetter Ignore<TSetter>(this TSetter setter, params string[] names) where TSetter : TypeAdapterSetter
        {
            setter.CheckCompiled();

            foreach (var name in names)
            {
                setter.Settings.IgnoreIfs[name] = null;
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

        public static TSetter Map<TSetter>(this TSetter setter, IEnumerable<KeyValuePair<string, string>> pairs) where TSetter : TypeAdapterSetter
        {
            setter.CheckCompiled();

            foreach (var item in pairs)
            {
                setter.Map(item.Key, item.Value);
            }
            return setter;
        }

        public static TSetter EnableNonPublicMembers<TSetter>(this TSetter setter, bool value) where TSetter : TypeAdapterSetter
        {
            setter.CheckCompiled();

            if (value)
            {
                setter.Settings.ShouldMapMember.Remove(ShouldMapMember.AllowPublic);
                setter.Settings.ShouldMapMember.Add(ShouldMapMember.AllowNonPublic);
            }
            else
            {
                setter.Settings.ShouldMapMember.Remove(ShouldMapMember.AllowNonPublic);
                setter.Settings.ShouldMapMember.Add(ShouldMapMember.AllowPublic);
            }

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

        public static TSetter GetMemberName<TSetter>(this TSetter setter, Func<IMemberModel, string> func) where TSetter : TypeAdapterSetter
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

        public static TSetter MapToConstructor<TSetter>(this TSetter setter, ConstructorInfo constructorInfo)
            where TSetter : TypeAdapterSetter
        {
            setter.CheckCompiled();

            setter.Settings.MapToConstructor = constructorInfo;
            return setter;
        }

        public static TSetter MaxDepth<TSetter>(this TSetter setter, int value) where TSetter : TypeAdapterSetter
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
                Settings.IgnoreIfs[ReflectionUtils.GetMemberPath(member, true)] = null;
            }
            return this;
        }

        public TypeAdapterSetter<TDestination> Map<TDestinationMember, TSourceMember>(
            Expression<Func<TDestination, TDestinationMember>> member,
            Expression<Func<TSourceMember>> source)
        {
            this.CheckCompiled();

            var invoker = Expression.Lambda(source.Body, Expression.Parameter(typeof (object)));
            Settings.Resolvers.Add(new InvokerModel
            {
                DestinationMemberName = ReflectionUtils.GetMemberPath(member),
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

            Settings.Resolvers.Add(new InvokerModel
            {
                DestinationMemberName = ReflectionUtils.GetMemberPath(destinationMember),
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
    }

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

        public  new TypeAdapterSetter<TSource, TDestination> Map<TDestinationMember, TSourceMember>(
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

        #endregion

        public TypeAdapterSetter<TSource, TDestination> IgnoreIf(
            Expression<Func<TSource, TDestination, bool>> condition,
            params Expression<Func<TDestination, object>>[] members)
        {
            this.CheckCompiled();

            foreach (var member in members)
            {
                var name = ReflectionUtils.GetMemberPath(member, true);
                Settings.IgnoreIfs.Merge(name, condition);
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
                Settings.IgnoreIfs.Merge(member, condition);
            }
            return this;
        }

        public TypeAdapterSetter<TSource, TDestination> Map<TDestinationMember, TSourceMember>(
            Expression<Func<TDestination, TDestinationMember>> member,
            Expression<Func<TSource, TSourceMember>> source, Expression<Func<TSource, bool>> shouldMap = null)
        {
            this.CheckCompiled();

            Settings.Resolvers.Add(new InvokerModel
            {
                DestinationMemberName = ReflectionUtils.GetMemberPath(member),
                Invoker = source,
                Condition = shouldMap
            });
            return this;
        }

        public TypeAdapterSetter<TSource, TDestination> Map<TSourceMember>(
            string memberName,
            Expression<Func<TSource, TSourceMember>> source, Expression<Func<TSource, bool>> shouldMap = null)
        {
            this.CheckCompiled();
            
            Settings.Resolvers.Add(new InvokerModel
            {
                DestinationMemberName = memberName,
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
                if (Settings.ConverterToTargetFactory == null)
                {
                    Settings.ConverterToTargetFactory = adapter.CreateAdaptToTargetFunc;
                }
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
            this.CheckCompiled();

            Config.Rules.Add(new TypeAdapterRule
            {
                Priority = arg =>
                    arg.SourceType == typeof(TDerivedSource) &&
                    arg.DestinationType == typeof(TDerivedDestination) ? (int?)100 : null,
                Settings = Settings
            });

            Settings.Includes.Add(new TypeTuple(typeof(TDerivedSource), typeof(TDerivedDestination)));

            return this;
        }

        public TypeAdapterSetter<TSource, TDestination> Inherits<TBaseSource, TBaseDestination>()
        {
            this.CheckCompiled();

            Type baseSourceType = typeof(TBaseSource);
            Type baseDestinationType = typeof(TBaseDestination);

            if (!baseSourceType.GetTypeInfo().IsAssignableFrom(typeof(TSource).GetTypeInfo()))
                throw new InvalidCastException("In order to use inherits, TSource must inherit directly or indirectly from TBaseSource.");

            if (!baseDestinationType.GetTypeInfo().IsAssignableFrom(typeof(TDestination).GetTypeInfo()))
                throw new InvalidCastException("In order to use inherits, TDestination must inherit directly or indirectly from TBaseDestination.");

            if (Config.RuleMap.TryGetValue(new TypeTuple(baseSourceType, baseDestinationType), out var rule))
            {
                Settings.Apply(rule.Settings);
            }
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
            SourceToDestinationSetter = config.ForType<TSource, TDestination>()
                .Unflattening(true);
            DestinationToSourceSetter = config.ForType<TDestination, TSource>()
                .Unflattening(true);
        }

        public TwoWaysTypeAdapterSetter<TSource, TDestination> AddDestinationTransform<TDestinationMember>(Expression<Func<TDestinationMember, TDestinationMember>> transform)
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
            DestinationToSourceSetter.IgnoreMember((model, side) => predicate(model, side == MemberSide.Source ? MemberSide.Destination : MemberSide.Source));
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

        public TwoWaysTypeAdapterSetter<TSource, TDestination> Map(IEnumerable<KeyValuePair<string, string>> pairs)
        {
            foreach (var item in pairs)
            {
                SourceToDestinationSetter.Map(item.Key, item.Value);
                DestinationToSourceSetter.Map(item.Value, item.Key);
            }

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

        public TwoWaysTypeAdapterSetter<TSource, TDestination> GetMemberName(Func<IMemberModel, string> func)
        {
            SourceToDestinationSetter.GetMemberName(func);
            DestinationToSourceSetter.GetMemberName(func);
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
                var path = ReflectionUtils.GetMemberPath(member, true);
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
    }
}
