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

        internal static readonly TypeAdapterConfigSettings ConfigSettings = new TypeAdapterConfigSettings();

        private TypeAdapterConfig()
        {
        }
        
        public static TypeAdapterGlobalSettings GlobalSettings
        {
            get { return _globalSettings; }
            set { _globalSettings = value; }
        }

        public static TypeAdapterConfig NewConfig()
        {
            ConfigSettings.NewInstanceForSameType = true;

            return new TypeAdapterConfig();
        }

        public TypeAdapterConfig NewInstanceForSameType(bool newInstanceForSameType)
        {
            ConfigSettings.NewInstanceForSameType = newInstanceForSameType;

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

        internal static object GetFromConfigurationCache(Type sourceType, Type destinationType)
        {
            var key = ReflectionUtils.GetHashKey(sourceType, destinationType);
            object returnValue;

            _configurationCache.TryGetValue(key, out returnValue);

            return returnValue;
        }

        internal static void ClearConfigurationCache()
        {
            _configurationCache.Clear();
        }

    }


    public class TypeAdapterConfig<TSource, TDestination> : IValidatableConfig
    {
        private static TypeAdapterConfigSettings<TSource, TDestination> _configSettings;
        private static bool _configSettingsSet;
        private static ProjectionConfig<TSource, TDestination> _projection;

        private TypeAdapterConfig()
        {
        }

        public static TypeAdapterConfig<TSource, TDestination> NewConfig()
        {
            if (_configSettings == null)
            {
                _configSettings = new TypeAdapterConfigSettings<TSource, TDestination>();
            }
            else
            {
                _configSettings.Reset();
            }

            ClassAdapter<TSource, TDestination>.Reset();
            _projection = ProjectionConfig<TSource, TDestination>.NewConfig();

            var config = new TypeAdapterConfig<TSource, TDestination>();
            TypeAdapterConfig.UpsertConfigurationCache(config);
            _configSettingsSet = true;

            return config;
        }

        internal static TypeAdapterConfigSettings<TSource, TDestination> ConfigSettings
        {
            get
            {
                if (_configSettingsSet)
                    return _configSettings;

                _configSettingsSet = true;

                return _configSettings ?? (_configSettings = DeriveConfigSettings());
            }
        }

        public static void Clear()
        {
            TypeAdapterConfig.RemoveFromConfigurationCache<TSource, TDestination>();
            _configSettings = null;
            _configSettingsSet = false;
            ClassAdapter<TSource, TDestination>.Reset();
            _projection = ProjectionConfig<TSource, TDestination>.NewConfig();
        }

        [Obsolete("Use Ignore instead.")]
        public TypeAdapterConfig<TSource, TDestination> IgnoreMember(params Expression<Func<TDestination, object>>[] members)
        {
            return Ignore(members);
        }

        [Obsolete("Use Ignore instead.")]
        public TypeAdapterConfig<TSource, TDestination> IgnoreMember(params string[] members)
        {
            return Ignore(members);
        }

        public TypeAdapterConfig<TSource, TDestination> Ignore(params Expression<Func<TDestination, object>>[] members)
        {
            _projection.IgnoreMember(members);

            if (members != null && members.Length > 0)
            {
                for (int i = 0; i < members.Length; i++)
                {
                    var memberExp = ReflectionUtils.GetMemberInfo(members[i]);
                    if (memberExp != null)
                    {
                        _configSettings.IgnoreMembers.Add(memberExp.Member.Name);
                    }
                }
            }
            return this;
        }

        public TypeAdapterConfig<TSource, TDestination> Ignore(params string[] members)
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
                    for (int i = 0; i < members.Length; i++)
                    {
                        _configSettings.IgnoreMembers.Add(members[i]);
                    }
                }
            }
            return this;
        }

        public TypeAdapterConfig<TSource, TDestination> MapWith<TConverter>() where TConverter : ITypeResolver<TSource, TDestination>, new()
        {
            _configSettings.ConverterFactory = () => new TConverter();
            return this;
        }

        public TypeAdapterConfig<TSource, TDestination> MapWith(ITypeResolver<TSource, TDestination> resolver)
        {
            _configSettings.ConverterFactory = () => resolver;
            return this;
        }

        public TypeAdapterConfig<TSource, TDestination> MapWith(Func<ITypeResolver<TSource, TDestination>> converterFactoryFunc)
        {
            _configSettings.ConverterFactory = converterFactoryFunc;
            return this;
        }

        public TypeAdapterConfig<TSource, TDestination> Resolve<TValueResolver, TDestinationMember>(Expression<Func<TDestination, TDestinationMember>> member,
            Expression<Func<TSource, bool>> shouldMap = null) 
            where TValueResolver : IValueResolver<TSource, TDestinationMember>, new()
        {
            return Map(member, src => new TValueResolver().Resolve(src), shouldMap);
        }

        public TypeAdapterConfig<TSource, TDestination> Resolve<TDestinationMember>(Expression<Func<TDestination, TDestinationMember>> member,
            Func<IValueResolver<TSource, TDestinationMember>> resolverFactory, 
            Expression<Func<TSource, bool>> shouldMap = null) 
        {
            return Map(member, src => resolverFactory().Resolve(src), shouldMap);
        }

        public TypeAdapterConfig<TSource, TDestination> Resolve<TDestinationMember>(
            Expression<Func<TDestination, TDestinationMember>> member, IValueResolver<TSource, TDestinationMember> resolver, 
            Expression<Func<TSource, bool>> shouldMap = null)
        {
            return Map(member, src => resolver.Resolve(src), shouldMap);
        }

        [Obsolete("Use Map instead.")]
        public TypeAdapterConfig<TSource, TDestination> MapFrom<TDestinationMember>(Expression<Func<TDestination, TDestinationMember>> member,
            Expression<Func<TSource, TDestinationMember>> source, Expression<Func<TSource, bool>> shouldMap = null)
        {
            return Map(member, source, shouldMap);
        }

        public TypeAdapterConfig<TSource, TDestination> Map<TDestinationMember>(Expression<Func<TDestination, TDestinationMember>> member,
            Expression<Func<TSource, TDestinationMember>> source, Expression<Func<TSource, bool>> shouldMap = null)
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

            _configSettings.Resolvers.Add(new InvokerModel<TSource>
            {
                MemberName = memberExp.Member.Name,
                Invoker = resolver,
                Condition = condition
            });

            return this;
        }

        public TypeAdapterConfig<TSource, TDestination> NewInstanceForSameType(bool newInstanceForSameType)
        {
            _configSettings.NewInstanceForSameType = newInstanceForSameType;

            return this;
        }

        public TypeAdapterConfig<TSource, TDestination> IgnoreNullValues(bool ignoreNullValues)
        {
            _configSettings.IgnoreNullValues = ignoreNullValues;

            return this;
        }

        public TypeAdapterConfig<TSource, TDestination> ConstructUsing(Func<TDestination> constructUsing)
        {
            _configSettings.ConstructUsing = constructUsing;

            return this;
        }

        public TypeAdapterConfig<TSource, TDestination> MaxDepth(int maxDepth)
        {
            _configSettings.MaxDepth = maxDepth;

            _projection.MaxDepth(maxDepth);

            return this;
        }

        public TransformsCollection DestinationTransforms
        {
            get
            {
                if (_configSettings == null)
                    return null;

                return _configSettings.DestinationTransforms;
            }
        }

        public void Validate()
        {
            var errorList = new List<string>();

            Validate(errorList);

            if(errorList.Count > 0)
                throw new ArgumentOutOfRangeException(string.Join(Environment.NewLine, errorList));
        }

        public bool Validate(List<string> errorList)
        {
            //Skip things that have a custom converter
            if (_configSettings.ConverterFactory != null)
                return true;

            bool isValid = true;

            var unmappedMembers = GetUnmappedMembers();

            if (unmappedMembers.Count > 0)
            {
                string message =
                    string.Format(
                        "The following members on destination({0}) are not represented in either mappings or in the source({1}):{2}",
                        typeof (TDestination).FullName, typeof (TSource).FullName, string.Join(", ", unmappedMembers));

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

            List<string> unmappedMembers = destType.GetPublicFieldsAndProperties(false).Select(x => x.Name).ToList();

            var sourceType = typeof (TSource);
            List<string> sourceMembers = sourceType.GetPublicFieldsAndProperties().Select(x => x.Name).ToList();

            //Remove items that have resolvers or are ignored
            unmappedMembers.RemoveAll(sourceMembers.Contains);
            unmappedMembers.RemoveAll(_configSettings.IgnoreMembers.Contains);
            unmappedMembers.RemoveAll(x => _configSettings.Resolvers.Any(r => r.MemberName == x));

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

            var unmappedMembers = destType.GetPublicFieldsAndProperties(false).ToList();

            //Remove items that have resolvers or are ignored
            unmappedMembers.RemoveAll(x => _configSettings.IgnoreMembers.Contains(x.Name));
            unmappedMembers.RemoveAll(x => _configSettings.Resolvers.Any(r => r.MemberName == x.Name));

            var sourceType = typeof (TSource);

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

                if (destMemberType == sourceMemberType)
                    continue;

                //See if the destination member has a mapping if needed
                if (!TypeAdapterConfig.ExistsInConfigurationCache(sourceMemberType, destMemberType))
                {
                    errorList.Add(
                        string.Format(
                            "Explicit Mapping is turned on and the following source({0}) and destination({1}) types do not have a mapping defined.",
                            sourceMemberType.FullName, destMemberType.FullName));
                }
            }

            return errorList;
        }

        private static TypeAdapterConfigSettings<TSource, TDestination> DeriveConfigSettings()
        {
            TypeAdapterConfigSettings<TSource, TDestination> configSettings = null;

            //See if we can convert inherited config settings.
            Type destType = typeof(TDestination);
            Type baseSourceType = typeof(TSource).BaseType;

            while (baseSourceType != null && !baseSourceType.IsPrimitiveRoot())
            {
                var baseConfig = TypeAdapterConfig.GetFromConfigurationCache(baseSourceType, destType);
                if (baseConfig != null)
                {
                    Type configType = typeof(TypeAdapterConfig<,>).MakeGenericType(baseSourceType, destType);
                    var property = configType.GetProperty("ConfigSettings", BindingFlags.Static | BindingFlags.NonPublic);

                    var baseConfigSettings = (TypeAdapterConfigSettingsBase)property.GetValue(baseConfig);

                    configSettings = new TypeAdapterConfigSettings<TSource, TDestination>
                    {
                        MaxDepth = baseConfigSettings.MaxDepth,
                        IgnoreNullValues = baseConfigSettings.IgnoreNullValues,
                        NewInstanceForSameType = baseConfigSettings.NewInstanceForSameType,
                    };
                    configSettings.IgnoreMembers.AddRange(baseConfigSettings.IgnoreMembers);
                    configSettings.DestinationTransforms.Upsert(baseConfigSettings.DestinationTransforms.Transforms);

                    List<object> resolvers = baseConfigSettings.GetResolversAsObjects();

                    Type baseInvokerType = typeof(InvokerModel<>).MakeGenericType(baseSourceType);

                    foreach (var baseResolver in resolvers)
                    {
                        var convertedResolver = new InvokerModel<TSource>();
                        convertedResolver.MemberName = (string)baseInvokerType.GetField("MemberName").GetValue(baseResolver);
                        convertedResolver.Invoker = (Func<TSource, object>)baseInvokerType.GetField("Invoker").GetValue(baseResolver);
                        convertedResolver.Condition = (Func<TSource, bool>)baseInvokerType.GetField("Condition").GetValue(baseResolver);

                        configSettings.Resolvers.Add(convertedResolver);
                    }

                    break;
                }

                baseSourceType = baseSourceType.BaseType;
            }

            return configSettings;
        }

    }

    public interface IValidatableConfig
    {
        bool Validate(List<string> errorList);

        void Validate();
    }
}
