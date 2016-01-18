using Mapster.Models;
using Mapster.Utils;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace Mapster
{
    public class TypeAdapterSetter
    {
        public readonly TypeAdapterSettings Settings;
        public readonly TypeAdapterConfig ParentConfig;
        internal TypeAdapterSetter(TypeAdapterSettings settings, TypeAdapterConfig parentConfig)
        {
            this.Settings = settings;
            this.ParentConfig = parentConfig;
        }
    }
    public static class TypeAdapterSetterExtensions
    {
        public static TSetter AddDestinationTransform<TSetter, TDestinationMember>(this TSetter setter, Expression<Func<TDestinationMember, TDestinationMember>> transform) where TSetter : TypeAdapterSetter
        {
            setter.Settings.DestinationTransforms.Upsert(transform);
            return setter;
        }

        public static TSetter Ignore<TSetter>(this TSetter setter, params string[] names) where TSetter : TypeAdapterSetter
        {
            setter.Settings.IgnoreMembers.UnionWith(names);
            return setter;
        }

        public static TSetter IgnoreAttribute<TSetter>(this TSetter setter, params Type[] types) where TSetter : TypeAdapterSetter
        {
            setter.Settings.IgnoreAttributes.UnionWith(types);
            return setter;
        }

        public static TSetter ShallowCopyForSameType<TSetter>(this TSetter setter, bool value) where TSetter : TypeAdapterSetter
        {
            setter.Settings.ShallowCopyForSameType = value;
            return setter;
        }

        public static TSetter IgnoreNullValues<TSetter>(this TSetter setter, bool value) where TSetter : TypeAdapterSetter
        {
            setter.Settings.IgnoreNullValues = value;
            return setter;
        }

        public static TSetter PreserveReference<TSetter>(this TSetter setter, bool value) where TSetter : TypeAdapterSetter
        {
            setter.Settings.PreserveReference = value;
            return setter;
        }

        public static TSetter NoInherit<TSetter>(this TSetter setter, bool value) where TSetter : TypeAdapterSetter
        {
            setter.Settings.NoInherit = value;
            return setter;
        }
    }
    public class TypeAdapterSetter<TSource, TDestination> : TypeAdapterSetter
    {
        internal TypeAdapterSetter(TypeAdapterSettings settings, TypeAdapterConfig parentConfig)
            : base(settings, parentConfig)
        { }

        public TypeAdapterSetter<TSource, TDestination> Ignore(params Expression<Func<TDestination, object>>[] members)
        {
            Settings.IgnoreMembers.UnionWith(members.Select(member => ReflectionUtils.GetMemberInfo(member).Member.Name));
            return this;
        }

        public TypeAdapterSetter<TSource, TDestination> Map<TDestinationMember, TSourceMember>(
            Expression<Func<TDestination, TDestinationMember>> member,
            Expression<Func<TSource, TSourceMember>> source, Expression<Func<TSource, bool>> shouldMap = null)
        {
            var memberExp = member.Body as MemberExpression;

            if (memberExp == null)
            {
                var ubody = (UnaryExpression)member.Body;
                memberExp = ubody.Operand as MemberExpression;
            }

            if (memberExp == null)
                throw new ArgumentException("argument must be member access", nameof(member));

            Settings.Resolvers.Add(new InvokerModel
            {
                MemberName = memberExp.Member.Name,
                Invoker = source,
                Condition = shouldMap
            });

            return this;
        }

        public TypeAdapterSetter<TSource, TDestination> ConstructUsing(Expression<Func<TSource, TDestination>> constructUsing)
        {
            Settings.ConstructUsing = constructUsing;

            return this;
        }

        public TypeAdapterSetter<TSource, TDestination> MapWith(Func<CompileArgument, Expression<Func<TSource, TDestination>>> converterFactory)
        {
            Settings.ConverterFactory = converterFactory;
            return this;
        }

        public TypeAdapterSetter<TSource, TDestination> MapToTargetWith(Func<CompileArgument, Expression<Func<TSource, TDestination, TDestination>>> converterFactory)
        {
            Settings.ConverterToTargetFactory = converterFactory;
            return this;
        }

        public TypeAdapterSetter<TSource, TDestination> Inherits<TBaseSource, TBaseDestination>()
        {
            Type baseSourceType = typeof(TBaseSource);
            Type baseDestinationType = typeof(TBaseDestination);

            if (!baseSourceType.IsAssignableFrom(typeof(TSource)))
                throw new InvalidCastException("In order to use inherits, TSource must inherit directly or indirectly from TBaseSource.");

            if (!baseDestinationType.IsAssignableFrom(typeof(TDestination)))
                throw new InvalidCastException("In order to use inherits, TDestination must inherit directly or indirectly from TBaseDestination.");

            TypeAdapterRule rule;
            if (ParentConfig.Dict.TryGetValue(new TypeTuple(baseSourceType, baseDestinationType), out rule))
            {
                Settings.Apply(rule.Settings);
            }
            return this;
        }

        public void Compile()
        {
            this.ParentConfig.Compile(typeof(TSource), typeof(TDestination));
        }
    }
}
