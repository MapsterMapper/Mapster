using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Mapster.Models;
using Mapster.Utils;

namespace Mapster
{

    public class TypeAdapterConfig
    {
        private static readonly ConcurrentDictionary<TypeTuple, object> _configurationCache = new ConcurrentDictionary<TypeTuple, object>();

        internal static readonly TypeAdapterConfigSettings ConfigSettings = new TypeAdapterConfigSettings();

        private TypeAdapterConfig()
        {
        }
        
        public static TypeAdapterGlobalSettings GlobalSettings { get; set; } = new TypeAdapterGlobalSettings();

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
            var key = new TypeTuple(typeof (TSource), typeof (TDestination));
            _configurationCache[key] = config;
        }

        internal static void RemoveFromConfigurationCache<TSource, TDestination>()
        {
            var key = new TypeTuple(typeof(TSource), typeof(TDestination));
            object obj;
            _configurationCache.TryRemove(key, out obj);
        }

        internal static bool ExistsInConfigurationCache(Type sourceType, Type destinationType)
        {
            var key = new TypeTuple(sourceType, destinationType);

            return _configurationCache.ContainsKey(key);
        }

        internal static TypeAdapterConfig<TSource, TDestination> GetFromConfigurationCache<TSource, TDestination>()
        {
            return (TypeAdapterConfig<TSource, TDestination>)GetFromConfigurationCache(typeof(TSource), typeof(TDestination));
        }

        internal static object GetFromConfigurationCache(Type sourceType, Type destinationType)
        {
            var key = new TypeTuple(sourceType, destinationType);
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

            _projection = ProjectionConfig<TSource, TDestination>.NewConfig();

            var config = new TypeAdapterConfig<TSource, TDestination>();
            TypeAdapterConfig.UpsertConfigurationCache(config);
            _configSettingsSet = false;

            return config;
        }

        internal static TypeAdapterConfigSettings<TSource, TDestination> ConfigSettings
        {
            get
            {
                if (_configSettingsSet)
                    return _configSettings;

                _configSettingsSet = true;

                if (_configSettings == null)
                    return _configSettings = DeriveConfigSettings();

                ApplyInheritedConfigSettings();

                return _configSettings;
            }
        }

        public void Recompile()
        {
            CallRecompile();
        }

        private static Action _recompile;
        private static void CallRecompile()
        {
            if (_recompile == null)
            {
                var method = typeof(TypeAdapter<,>).MakeGenericType(typeof(TSource), typeof(TDestination)).GetMethod("Recompile");
                _recompile = Expression.Lambda<Action>(Expression.Call(method)).Compile();
            }
            _recompile();
        }

        public static void Clear(bool noRecompile = false)
        {
            TypeAdapterConfig.RemoveFromConfigurationCache<TSource, TDestination>();
            _configSettings = null;
            _configSettingsSet = false;
            _projection = ProjectionConfig<TSource, TDestination>.NewConfig();

            if (!noRecompile)
                CallRecompile();
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

            _configSettings.Resolvers.Add(new InvokerModel
            {
                MemberName = memberExp.Member.Name,
                Invoker = source,
                Condition = shouldMap
            });

            return this;
        }

        public TypeAdapterConfig<TSource, TDestination> Inherits<TBaseSource, TBaseDestination>()
        {
            Type baseSourceType = typeof (TBaseSource);
            Type baseDestinationType = typeof (TBaseDestination);

            if (!baseSourceType.IsAssignableFrom(typeof(TSource)))
                throw new InvalidCastException("In order to use inherits, TSource must inherit directly or indirectly from TBaseSource.");

            if (!baseDestinationType.IsAssignableFrom(typeof(TDestination)))
                throw new InvalidCastException("In order to use inherits, TDestination must inherit directly or indirectly from TBaseDestination.");

            _configSettings.InheritedSourceType = baseSourceType;
            _configSettings.InheritedDestinationType = baseDestinationType;
            return this;
        }

        public TypeAdapterConfig<TSource, TDestination> ConstructUsing(Expression<Func<TDestination>> constructUsing)
        {
            _configSettings.ConstructUsing = constructUsing;

            return this;
        }

        public TypeAdapterConfig<TSource, TDestination> SameInstanceForSameType(bool sameInstanceForSameType)
        {
            _configSettings.SameInstanceForSameType = sameInstanceForSameType;

            return this;
        }

        public TypeAdapterConfig<TSource, TDestination> IgnoreNullValues(bool ignoreNullValues)
        {
            _configSettings.IgnoreNullValues = ignoreNullValues;

            return this;
        }

        public TypeAdapterConfig<TSource, TDestination> CircularReferenceCheck(bool isCheck)
        {
            _configSettings.CircularReferenceCheck = isCheck;

            return this;
        }

        public TypeAdapterConfig<TSource, TDestination> MaxProjectionDepth(int maxDepth)
        {
            _projection.MaxDepth(maxDepth);

            return this;
        }

        public TransformsCollection DestinationTransforms
        {
            get
            {
                return _configSettings?.DestinationTransforms;
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
                    $"The following members on destination({typeof (TDestination).FullName}) are not represented in either mappings or in the source({typeof (TSource).FullName}):{string.Join(", ", unmappedMembers)}";

                errorList?.Add(message);
                isValid = false;
            }

            if (TypeAdapterConfig.GlobalSettings.RequireExplicitMapping)
            {
                errorList?.AddRange(GetMissingExplicitMappings());
                isValid = false;
            }

            return isValid;
        }

        private static List<string> GetUnmappedMembers()
        {
            var destType = typeof (TDestination);

            List<MemberInfo> unmappedMembers = destType.GetPublicFieldsAndProperties(false).ToList();

            var sourceType = typeof (TSource);
            List<string> sourceMembers = sourceType.GetPublicFieldsAndProperties().Select(x => x.Name).ToList();

            //Remove items that have resolvers or are ignored
            unmappedMembers.RemoveAll(x => sourceMembers.Contains(x.Name));

            RemoveInheritedExplicitMappings<TSource>(unmappedMembers, sourceType, destType);

            unmappedMembers.RemoveAll(x => sourceType.GetMethod("Get" + x.Name) != null);

            unmappedMembers.RemoveAll(x =>
            {
                var source = Expression.Parameter(sourceType);
                var exp = ReflectionUtils.GetDeepFlattening(source, x.Name);
                return exp != null;
            });

            return unmappedMembers.Select(x => x.Name).ToList();
        }


        private static void RemoveInheritedExplicitMappings<TOriginalSource>(List<MemberInfo>unmappedMembers, Type sourceType, Type destType)
        {
            var config = TypeAdapterConfig.GetFromConfigurationCache(sourceType, destType);

            Type configType = typeof(TypeAdapterConfig<,>).MakeGenericType(sourceType, destType);
            var property = configType.GetProperty("ConfigSettings", BindingFlags.Static | BindingFlags.NonPublic);

            var configSettings = (TypeAdapterConfigSettingsBase)property.GetValue(config);

            if (configSettings != null)
            {

                //Remove items that have resolvers or are ignored
                unmappedMembers.RemoveAll(x => configSettings.IgnoreMembers.Contains(x.Name));

                List<object> resolverObjects = configSettings.GetResolversAsObjects();

                Type baseInvokerType = typeof(InvokerModel);

                foreach (var resolverObject in resolverObjects)
                {
                    var memberName = (string) baseInvokerType.GetField("MemberName").GetValue(resolverObject);
                    unmappedMembers.RemoveAll(x => x.Name == memberName);
                }

                if (unmappedMembers.Count == 0)
                    return;

                if (configSettings.InheritedDestinationType != null &&
                    configSettings.InheritedSourceType != null)
                {
                    RemoveInheritedExplicitMappings<TOriginalSource>(unmappedMembers, configSettings.InheritedSourceType,
                        configSettings.InheritedDestinationType);
                }
            }
        }


        private static List<string> GetMissingExplicitMappings()
        {
            var errorList = new List<string>();

            var destType = typeof (TDestination);
            var sourceType = typeof(TSource);

            var unmappedMembers = destType.GetPublicFieldsAndProperties(false).ToList();

            RemoveInheritedExplicitMappings<TSource>(unmappedMembers, sourceType, destType);

            //Remove items that have resolvers or are ignored
            unmappedMembers.RemoveAll(x => _configSettings.IgnoreMembers.Contains(x.Name));
            unmappedMembers.RemoveAll(x => _configSettings.Resolvers.Any(r => r.MemberName == x.Name));

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
                        $"Explicit Mapping is turned on and the following source({sourceMemberType.FullName}) and destination({destMemberType.FullName}) types do not have a mapping defined.");
                }
            }

            return errorList;
        }

        private static TypeAdapterConfigSettings<TSource, TDestination> DeriveConfigSettings()
        {
            TypeAdapterConfigSettings<TSource, TDestination> configSettings = null;

            //See if we can convert inherited config settings.
            Type destType = typeof(TDestination);
            Type sourceType = typeof(TSource).BaseType;
            bool matchFound = false;

            while (destType != null && !destType.IsPrimitiveRoot())
            {
                while (sourceType != null && !sourceType.IsPrimitiveRoot())
                {
                    var baseConfig = TypeAdapterConfig.GetFromConfigurationCache(sourceType, destType);
                    if (baseConfig != null)
                    {
                        Type configType = typeof(TypeAdapterConfig<,>).MakeGenericType(sourceType, destType);
                        var property = configType.GetProperty("ConfigSettings", BindingFlags.Static | BindingFlags.NonPublic);

                        var baseConfigSettings = (TypeAdapterConfigSettingsBase)property.GetValue(baseConfig);

                        configSettings = new TypeAdapterConfigSettings<TSource, TDestination>
                        {
                            CircularReferenceCheck = baseConfigSettings.CircularReferenceCheck,
                            IgnoreNullValues = baseConfigSettings.IgnoreNullValues,
                            SameInstanceForSameType = baseConfigSettings.SameInstanceForSameType
                        };

                        configSettings.IgnoreMembers.AddRange(baseConfigSettings.IgnoreMembers);
                        
                        configSettings.DestinationTransforms.Upsert(baseConfigSettings.DestinationTransforms.Transforms);

                        List<object> resolvers = baseConfigSettings.GetResolversAsObjects();

                        Type baseInvokerType = typeof(InvokerModel);

                        foreach (var baseResolver in resolvers)
                        {
                            var convertedResolver = new InvokerModel
                            {
                                MemberName = (string) baseInvokerType.GetField("MemberName").GetValue(baseResolver),
                                Invoker = (Expression) baseInvokerType.GetField("Invoker").GetValue(baseResolver),
                                Condition = (Expression) baseInvokerType.GetField("Condition").GetValue(baseResolver)
                            };

                            configSettings.Resolvers.Add(convertedResolver);
                        }
                        matchFound = true;
                        break;
                    }
                    sourceType = sourceType.BaseType;
                }

                if (!matchFound && TypeAdapterConfig.GlobalSettings.AllowImplicitDestinationInheritance)
                {
                    destType = destType.BaseType;
                    sourceType = typeof(TSource);
                }
                else
                {
                    destType = null;
                }

            }

            return configSettings;
        }

        private static void ApplyInheritedConfigSettings()
        {
            if (_configSettings.InheritedSourceType == null || _configSettings.InheritedDestinationType == null)
                return;

            var baseConfig = TypeAdapterConfig.GetFromConfigurationCache(_configSettings.InheritedSourceType, _configSettings.InheritedDestinationType);
            if (baseConfig != null)
            {
                Type configType = typeof (TypeAdapterConfig<,>).MakeGenericType(_configSettings.InheritedSourceType, _configSettings.InheritedDestinationType);
                var property = configType.GetProperty("ConfigSettings", BindingFlags.Static | BindingFlags.NonPublic);

                var baseConfigSettings = (TypeAdapterConfigSettingsBase) property.GetValue(baseConfig);

                if (_configSettings.IgnoreNullValues == null)
                    _configSettings.IgnoreNullValues = baseConfigSettings.IgnoreNullValues;
                if (_configSettings.CircularReferenceCheck == null)
                    _configSettings.CircularReferenceCheck = baseConfigSettings.CircularReferenceCheck;
                if (_configSettings.SameInstanceForSameType == null)
                    _configSettings.SameInstanceForSameType = baseConfigSettings.SameInstanceForSameType;

                foreach (var ignoreMember in baseConfigSettings.IgnoreMembers)
                {
                    if(!_configSettings.IgnoreMembers.Contains(ignoreMember)
                        && _configSettings.Resolvers.All(x => x.MemberName != ignoreMember))
                        _configSettings.IgnoreMembers.Add(ignoreMember);
                }

                _configSettings.DestinationTransforms.TryAdd(baseConfigSettings.DestinationTransforms.Transforms);

                List<object> resolvers = baseConfigSettings.GetResolversAsObjects();

                Type baseInvokerType = typeof(InvokerModel);

                foreach (var baseResolver in resolvers)
                {
                    string memberName = (string)baseInvokerType.GetField("MemberName").GetValue(baseResolver);

                    if (_configSettings.Resolvers.All(x => x.MemberName != memberName))
                    {
                        var convertedResolver = new InvokerModel
                        {
                            MemberName = memberName,
                            Invoker = (Expression) baseInvokerType.GetField("Invoker").GetValue(baseResolver),
                            Condition = (Expression) baseInvokerType.GetField("Condition").GetValue(baseResolver)
                        };

                        _configSettings.Resolvers.Add(convertedResolver);    
                    }
                }
            }
            else
            {
                throw new ArgumentOutOfRangeException(
                    $"The configuration of source {typeof (TSource).FullName} to destination {typeof (TDestination).FullName} relies on explicit inheritance from a configuration with source {_configSettings.InheritedSourceType.FullName}" +
                    $"and destination {_configSettings.InheritedDestinationType.FullName}, which does not exist.");
            }

            //See if this config exists
        }

    }

    public interface IValidatableConfig
    {
        bool Validate(List<string> errorList);

        void Validate();
    }
}
