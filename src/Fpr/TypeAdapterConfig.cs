using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Fpr.Adapters;
using Fpr.Models;
using Fpr.Utils;

namespace Fpr
{

    public class TypeAdapterConfig
    {
        private static TypeAdapterGlobalSettings _globalSettings = new TypeAdapterGlobalSettings();

        private static readonly object _syncLock = new object(); 
        private static readonly Dictionary<int, object> _configurationCache = new Dictionary<int, object>(); 

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

        public static void Validate()
        {
            var errorList = new List<string>();
            foreach (var config in _configurationCache)
            {
                ((IValidatableConfig) config.Value).Validate(errorList);
            }

            if (errorList.Count > 0)
            {
                throw new ArgumentOutOfRangeException(string.Join(Environment.NewLine, errorList));
            }
        }

        internal static void UpsertConfigurationCache<TSource, TDestination>(
            TypeAdapterConfig<TSource, TDestination> config)
        {
            var key = ReflectionUtils.GetHashKey(typeof (TSource), typeof (TDestination));
            lock (_syncLock)
            {
                if (_configurationCache.ContainsKey(key))
                {
                    _configurationCache[key] = config;
                }
                else
                {
                    _configurationCache.Add(key, config);
                }
            }
        }

        internal static void RemoveFromConfigurationCache<TSource, TDestination>()
        {
            var key = ReflectionUtils.GetHashKey(typeof(TSource), typeof(TDestination));
            if (_configurationCache.ContainsKey(key))
            {
                lock (_syncLock)
                {
                    if (_configurationCache.ContainsKey(key))
                    {
                        _configurationCache.Remove(key);
                    }
                }
            }
        }

        internal static bool ExistsInConfigurationCache(Type sourceType, Type destinationType)
        {
            var key = ReflectionUtils.GetHashKey(sourceType, destinationType);

            return _configurationCache.ContainsKey(key);
        }

        internal static void ClearConfigurationCache()
        {
            _configurationCache.Clear();
        }

    }


    public class TypeAdapterConfig<TSource, TDestination> : IValidatableConfig
    {
        internal static TypeAdapterConfigSettings<TSource, TDestination> ConfigSettings;

        private readonly ProjectionConfig<TSource, TDestination> _projection =
            ProjectionConfig<TSource, TDestination>.NewConfig();

        private TypeAdapterConfig()
        {
        }

        public static TypeAdapterConfig<TSource, TDestination> NewConfig()
        {
            if (ConfigSettings == null)
            {
                ConfigSettings = new TypeAdapterConfigSettings<TSource, TDestination>();
            }
            else
            {
                ConfigSettings.Reset();
            }
            ClassAdapter<TSource, TDestination>.Reset();

            var config = new TypeAdapterConfig<TSource, TDestination>();
            TypeAdapterConfig.UpsertConfigurationCache(config);
            return config;
        }

        public static void Clear()
        {
            TypeAdapterConfig.RemoveFromConfigurationCache<TSource, TDestination>();
            ConfigSettings = null;
            ClassAdapter<TSource, TDestination>.Reset();
        }

        public TypeAdapterConfig<TSource, TDestination> IgnoreMember(
            params Expression<Func<TDestination, object>>[] members)
        {
            _projection.IgnoreMember(members);

            if (members != null && members.Length > 0)
            {
                var config = ConfigSettings;
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
                members =
                    typeof (TDestination).GetProperties()
                        .Where(p => members.Contains(p.Name))
                        .Select(p => p.Name)
                        .ToArray();

                if (members.Length > 0)
                {
                    var config = ConfigSettings;
                    for (int i = 0; i < members.Length; i++)
                    {
                        config.IgnoreMembers.Add(members[i]);
                    }
                }
            }
            return this;
        }

        public TypeAdapterConfig<TSource, TDestination> MapFrom<TKey>(Expression<Func<TDestination, TKey>> member,
            Expression<Func<TSource, TKey>> source,
            Expression<Func<TSource, bool>> shouldMap = null)
        {
            _projection.MapFrom(member, source);

            if (source == null)
                return this;

            var memberExp = member.Body as MemberExpression;

            if (memberExp == null)
            {
                var ubody = (UnaryExpression) member.Body;
                memberExp = ubody.Operand as MemberExpression;
            }

            if (memberExp == null)
                return this;

            var func = source.Compile();
            Func<TSource, object> resolver = src => func(src);

            Func<TSource, bool> condition = shouldMap != null ? shouldMap.Compile() : null;

            ConfigSettings.Resolvers.Add(new InvokerModel<TSource>
            {
                MemberName = memberExp.Member.Name,
                Invoker = resolver,
                Condition = condition
            });

            return this;
        }

        public TypeAdapterConfig<TSource, TDestination> NewInstanceForSameType(bool newInstanceForSameType)
        {
            ConfigSettings.NewInstanceForSameType = newInstanceForSameType;

            return this;
        }

        public TypeAdapterConfig<TSource, TDestination> IgnoreNullValues(bool ignoreNullValues)
        {
            ConfigSettings.IgnoreNullValues = ignoreNullValues;

            return this;
        }

        public TypeAdapterConfig<TSource, TDestination> ConstructUsing(Func<TDestination> constructUsing)
        {
            ConfigSettings.ConstructUsing = constructUsing;

            return this;
        }

        public TypeAdapterConfig<TSource, TDestination> MaxDepth(int maxDepth)
        {
            ConfigSettings.MaxDepth = maxDepth;

            _projection.MaxDepth(maxDepth);

            return this;
        }

        public TransformsCollection DestinationTransforms
        {
            get
            {
                if (ConfigSettings == null)
                    return null;

                return ConfigSettings.DestinationTransforms;
            }
        }

        public void Validate()
        {
            var errorList = new List<string>();

            Validate(errorList);

            if (TypeAdapterConfig.GlobalSettings.RequireExplicitMapping)
            {
                errorList.AddRange(GetMissingExplicitMappings());
            }

            if(errorList.Count > 0)
                throw new ArgumentOutOfRangeException(string.Join(Environment.NewLine, errorList));
        }

        public bool Validate(List<string> errorList)
        {
            bool isValid = true;
            var unmappedMembers = GetUnmappedMembers();

            if (unmappedMembers.Count > 0)
            {
                string message =
                    string.Format(
                        "The following members on destination({0}) are not represented in either mappings or in the source({1}):{2}",
                        typeof (TDestination).Name, typeof (TSource).Name, string.Join(", ", unmappedMembers));

                if (errorList != null)
                {
                    errorList.Add(message);
                }
                isValid = false;
            }

            if (TypeAdapterConfig.GlobalSettings.RequireExplicitMapping)
            {
                if (errorList != null)
                {
                    errorList.AddRange(GetMissingExplicitMappings());
                }
                isValid = false;
            }

            return isValid;
        }

        private List<string> GetUnmappedMembers()
        {
            var destType = typeof (TDestination);

            List<string> unmappedMembers = destType.GetPublicFieldsAndProperties().Select(x => x.Name).ToList();

            var sourceType = typeof (TSource);
            List<string> sourceMembers = sourceType.GetPublicFieldsAndProperties().Select(x => x.Name).ToList();

            //Remove items that have resolvers or are ignored
            unmappedMembers.RemoveAll(sourceMembers.Contains);
            unmappedMembers.RemoveAll(ConfigSettings.IgnoreMembers.Contains);
            unmappedMembers.RemoveAll(x => ConfigSettings.Resolvers.Any(r => r.MemberName == x));

            unmappedMembers.RemoveAll(x => sourceType.GetMethod("Get" + x) != null);

            unmappedMembers.RemoveAll(x =>
            {
                var delegates = new List<GenericGetter>();
                ReflectionUtils.GetDeepFlattening(sourceType, x, delegates);
                return (delegates.Count > 0);
            });

            return unmappedMembers;
        }


        private List<string> GetMissingExplicitMappings()
        {
            var errorList = new List<string>();

            var destType = typeof (TDestination);

            var unmappedMembers = destType.GetPublicFieldsAndProperties().ToList();

            unmappedMembers.RemoveAll(x => ConfigSettings.IgnoreMembers.Contains(x.Name));
            unmappedMembers.RemoveAll(x => ConfigSettings.Resolvers.Any(r => r.MemberName == x.Name));

            var sourceType = typeof (TSource);

            //Remove items that have resolvers or are ignored
            var sourceMembers = sourceType.GetPublicFieldsAndProperties().ToList();

            foreach (var sourceMember in sourceMembers)
            {
                var destMemberInfo = unmappedMembers.FirstOrDefault(x => x.Name == sourceMember.Name);

                if (destMemberInfo == null)
                    continue;

                unmappedMembers.Remove(destMemberInfo);

                Type destMemberType = destMemberInfo.GetMemberType();
                if (destMemberType.IsCollection())
                {
                    destMemberType = destMemberType.ExtractCollectionType();
                }

                if (destMemberType.IsPrimitiveRoot())
                    continue;

                Type sourceMemberType = sourceMember.GetMemberType();
                if (sourceMemberType.IsCollection())
                {
                    sourceMemberType = sourceMemberType.ExtractCollectionType();
                }

                //See if the destination member has a mapping if needed
                if (!TypeAdapterConfig.ExistsInConfigurationCache(sourceMemberType, destMemberType))
                {
                    errorList.Add(
                        string.Format(
                            "Explicit Mapping is turned on and the following source({0}) and destination({1}) types do not have a mapping defined.",
                            sourceMemberType.Name, destMemberType.Name));
                }
            }

            return errorList;
        }

    }

    public interface IValidatableConfig
    {
        bool Validate(List<string> errorList);

        void Validate();
    }
}
