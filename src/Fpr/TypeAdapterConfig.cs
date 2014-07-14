using System;
using System.Linq;
using System.Linq.Expressions;
using Fpr.Adapters;
using Fpr.Models;
using Fpr.Utils;

namespace Fpr
{

    public class TypeAdapterConfig
    {
        private static TypeAdapterGlobalSettings _globalSettings = new TypeAdapterGlobalSettings();

        private TypeAdapterConfig()
        {
        }
        
        public static TypeAdapterGlobalSettings GlobalSettings
        {
            get { return _globalSettings; }
            set { _globalSettings = value; }
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
        internal static TypeAdapterConfigSettings<TSource, TDestination> Configuration;

        private readonly ProjectionConfig<TSource, TDestination> _projection = ProjectionConfig<TSource, TDestination>.NewConfig();


        private TypeAdapterConfig()
        {
        }

        public static TypeAdapterConfig<TSource, TDestination> NewConfig()
        {
            if (Configuration == null)
            {
                Configuration = new TypeAdapterConfigSettings<TSource, TDestination>();
            }
            else
            {
                Configuration.Reset();
            }
            ClassAdapter<TSource,TDestination>.Reset();
            return new TypeAdapterConfig<TSource, TDestination>();
        }

        public static void Clear()
        {
            Configuration = null;
            ClassAdapter<TSource, TDestination>.Reset();
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

        public TypeAdapterConfig<TSource, TDestination> ConstructUsing(Func<TDestination> constructUsing)
        {
            Configuration.ConstructUsing = constructUsing;

            return this;
        }

        public TypeAdapterConfig<TSource, TDestination> DefaultEnumsOnNullOrEmptyString(bool defaultEnumsOnNullOrEmptyString)
        {
            Configuration.DefaultEnumsOnNullOrEmptyString = defaultEnumsOnNullOrEmptyString;

            return this;
        }

        public TypeAdapterConfig<TSource, TDestination> MaxDepth(int maxDepth)
        {
            Configuration.MaxDepth = maxDepth;

            _projection.MaxDepth(maxDepth);

            return this;
        }

        public TransformsCollection DestinationTransforms
        {
            get
            {
                if (Configuration == null)
                    return null;

                return Configuration.DestinationTransforms;
            }
        }

    }
}
