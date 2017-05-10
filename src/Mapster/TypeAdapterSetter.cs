using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Mapster.Adapters;
using Mapster.Models;

namespace Mapster
{
    public class TypeAdapterSetter
    {
        public readonly TypeAdapterSettings Settings;
        public readonly TypeAdapterConfig ParentConfig;
        public TypeAdapterSetter(TypeAdapterSettings settings, TypeAdapterConfig parentConfig)
        {
            this.Settings = settings;
            this.ParentConfig = parentConfig;
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
                setter.Settings.Ignores.Add(member => member.GetCustomAttributes(true).Any(attr => attr.GetType() == type));
            }
            return setter;
        }

        public static TSetter IgnoreMember<TSetter>(this TSetter setter, Func<IMemberModel, bool> predicate) where TSetter : TypeAdapterSetter
        {
            setter.CheckCompiled();

            setter.Settings.Ignores.Add(predicate);
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

        public static TSetter Map<TSetter, TSource, TSourceMember>(
            this TSetter setter, string memberName,
            Expression<Func<TSource, TSourceMember>> source, Expression<Func<TSource, bool>> shouldMap = null) where TSetter : TypeAdapterSetter
        {
            setter.CheckCompiled();

            setter.Settings.Resolvers.Add(new InvokerModel
            {
                MemberName = memberName,
                Invoker = source,
                Condition = shouldMap
            });
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
                Settings.IgnoreIfs[ReflectionUtils.GetMemberInfo(member).Member.Name] = null;
            }
            return this;
        }

        public TypeAdapterSetter<TDestination> Map<TDestinationMember, TSourceMember>(
            Expression<Func<TDestination, TDestinationMember>> member,
            Expression<Func<TSourceMember>> source)
        {
            this.CheckCompiled();

            var memberExp = ReflectionUtils.GetMemberInfo(member);
            var invoker = Expression.Lambda(source.Body, Expression.Parameter(typeof (object)), source.Parameters[0]);
            Settings.Resolvers.Add(new InvokerModel
            {
                MemberName = memberExp.Member.Name,
                Invoker = invoker,
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

        public new TypeAdapterSetter<TSource, TDestination> Ignore(params Expression<Func<TDestination, object>>[] members)
        {
            this.CheckCompiled();

            foreach (var member in members)
            {
                Settings.IgnoreIfs[ReflectionUtils.GetMemberInfo(member).Member.Name] = null;
            }
            return this;
        }

        public TypeAdapterSetter<TSource, TDestination> IgnoreIf(
            Expression<Func<TSource, TDestination, bool>> condition,
            params Expression<Func<TDestination, object>>[] members)
        {
            this.CheckCompiled();

            foreach (var member in members)
            {
                var name = ReflectionUtils.GetMemberInfo(member).Member.Name;
                Settings.IgnoreIfs.Merge(name, condition);
            }
            return this;
        }

        public TypeAdapterSetter<TSource, TDestination> Map<TDestinationMember, TSourceMember>(
            Expression<Func<TDestination, TDestinationMember>> member,
            Expression<Func<TSource, TSourceMember>> source, Expression<Func<TSource, bool>> shouldMap = null)
        {
            this.CheckCompiled();

            var memberExp = ReflectionUtils.GetMemberInfo(member);
            Settings.Resolvers.Add(new InvokerModel
            {
                MemberName = memberExp.Member.Name,
                Invoker = source,
                Condition = shouldMap
            });
            return this;
        }

        public TypeAdapterSetter<TSource, TDestination> TwoWaysMap<TDestinationMember, TSourceMember>(
            Expression<Func<TDestination, TDestinationMember>> member,
            Expression<Func<TSource, TSourceMember>> source)
        {
            this.CheckCompiled();

            Map(member, source);
            ParentConfig.ForType<TDestination, TSource>().Map(source, member);

            return this;
        }

        public TypeAdapterSetter<TSource, TDestination> Map<TSourceMember>(
            string memberName,
            Expression<Func<TSource, TSourceMember>> source, Expression<Func<TSource, bool>> shouldMap = null)
        {
            this.CheckCompiled();

            Settings.Resolvers.Add(new InvokerModel
            {
                MemberName = memberName,
                Invoker = source,
                Condition = shouldMap
            });
            return this;
        }

        public TypeAdapterSetter<TSource, TDestination> EnableNonPublicMembers()
        {
            this.CheckCompiled();

            var adapter = new ClassWithNonPublicMemberAdapter();
            Settings.ConverterFactory = adapter.CreateAdaptFunc;
            Settings.ConverterToTargetFactory = adapter.CreateAdaptToTargetFunc;
            Settings.ValueAccessingStrategies.Add(ValueAccessingStrategy.NonPublicPropertyOrField);

            return this;
        }

        public TypeAdapterSetter<TSource, TDestination> ConstructUsing(Expression<Func<TSource, TDestination>> constructUsing)
        {
            this.CheckCompiled();

            Settings.ConstructUsingFactory = arg => constructUsing;

            return this;
        }

        public TypeAdapterSetter<TSource, TDestination> MapWith(Expression<Func<TSource, TDestination>> converterFactory)
        {
            this.CheckCompiled();

            Settings.ConverterFactory = arg => converterFactory;

            if (Settings.ConverterToTargetFactory == null)
            {
                var dest = Expression.Parameter(typeof (TDestination));
                Settings.ConverterToTargetFactory = arg => Expression.Lambda(converterFactory.Body, converterFactory.Parameters[0], dest);
            }

            return this;
        }

        public TypeAdapterSetter<TSource, TDestination> MapToTargetWith(Expression<Func<TSource, TDestination, TDestination>> converterFactory)
        {
            this.CheckCompiled();

            Settings.ConverterToTargetFactory = arg => converterFactory;
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

        public TypeAdapterSetter<TSource, TDestination> Include<TDerivedSource, TDerivedDestination>() 
            where TDerivedSource: TSource
            where TDerivedDestination: TDestination
        {
            this.CheckCompiled();

            ParentConfig.Rules.Add(new TypeAdapterRule
            {
                Priority = (sourceType, destinationType, mapType) =>
                    sourceType == typeof(TDerivedSource) &&
                    destinationType == typeof(TDerivedDestination) ? (int?)100 : null,
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

            if (ParentConfig.RuleMap.TryGetValue(new TypeTuple(baseSourceType, baseDestinationType), out var rule))
            {
                Settings.Apply(rule.Settings);
            }
            return this;
        }

        public void Compile()
        {
            this.ParentConfig.Compile(typeof(TSource), typeof(TDestination));
        }

        public void CompileProjection()
        {
            this.ParentConfig.CompileProjection(typeof(TSource), typeof(TDestination));
        }
    }
}
