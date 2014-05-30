using System;
using System.Linq;
using System.Linq.Expressions;
using Fapper.Models;
using Fapper.Utils;

namespace Fapper
{

    public class TypeAdapterConfig
    {
        private TypeAdapterConfig()
        {
        }

        internal static readonly TypeAdapterConfigSettings Configuration = new TypeAdapterConfigSettings();

        public static TypeAdapterConfig NewConfig()
        {
            Configuration.NewInstanceForSameType = true;

            return new TypeAdapterConfig();
        }

        public TypeAdapterConfig NewInstanceForSameType(bool newInstanceForSameType)
        {
            Configuration.NewInstanceForSameType = newInstanceForSameType;

            return this;
        }
    }


    public class TypeAdapterConfig<TSource, TDestination>
    {

        internal static readonly TypeAdapterConfigSettings<TSource> Configuration = new TypeAdapterConfigSettings<TSource>();

        private readonly ProjectionConfig<TSource, TDestination> _projection = ProjectionConfig<TSource, TDestination>.NewConfig();


        private TypeAdapterConfig()
        {
        }

        public static TypeAdapterConfig<TSource, TDestination> NewConfig()
        {
            Configuration.Reset();
            TypeAdapter.Reset<TSource, TDestination>();
            
            return new TypeAdapterConfig<TSource, TDestination>();
        }


        public TypeAdapterConfig<TSource, TDestination> IgnoreMember(params Expression<Func<TDestination, object>>[] members)
        {
            _projection.IgnoreMember(members);

            if (members != null && members.Length > 0)
            {
                var config = Configuration;
                for (int i = 0; i < members.Length; i++)
                {
                    var memberExp = ReflectionUtils.GetMemberInfo(members[i]);
                    if (memberExp != null)
                    {
                        config.IgnoreMembers.Add(memberExp.Member.Name);
                    }
                }
            }
            return this;
        }

        public TypeAdapterConfig<TSource, TDestination> IgnoreMember(params string[] members)
        {
            _projection.IgnoreMember(members);

            if (members != null && members.Length > 0)
            {
                members = typeof(TDestination).GetProperties().Where(p => members.Contains(p.Name)).Select(p => p.Name).ToArray();

                if (members.Length > 0)
                {
                    var config = Configuration;
                    for (int i = 0; i < members.Length; i++)
                    {
                        config.IgnoreMembers.Add(members[i]);
                    }
                }
            }
            return this;
        }

        public TypeAdapterConfig<TSource, TDestination> MapFrom<TKey>(Expression<Func<TDestination, TKey>> member, Expression<Func<TSource, TKey>> source,
           Expression<Func<TSource, bool>> shouldMap = null)
        {
            _projection.MapFrom(member, source);

            if (source == null)
                return this;

            var memberExp = member.Body as MemberExpression;
            if (memberExp == null)
                return this;

            var func = source.Compile();
            Func<TSource, object> resolver = src => func(src);

            Func<TSource, bool> condition = shouldMap != null ? shouldMap.Compile() : null;

            Configuration.Resolvers.Add(new InvokerModel<TSource> { MemberName = memberExp.Member.Name, Invoker = resolver, Condition = condition });

            return this;
        }

        public TypeAdapterConfig<TSource, TDestination> NewInstanceForSameType(bool newInstanceForSameType)
        {
            Configuration.NewInstanceForSameType = newInstanceForSameType;

            return this;
        }

        public TypeAdapterConfig<TSource, TDestination> IgnoreNullValues(bool ignoreNullValues)
        {
            Configuration.IgnoreNullValues = ignoreNullValues;

            return this;
        }

        public TypeAdapterConfig<TSource, TDestination> MaxDepth(int maxDepth)
        {
            Configuration.MaxDepth = maxDepth;

            _projection.MaxDepth(maxDepth);

            return this;
        }

    }
}
