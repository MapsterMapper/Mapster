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
        private static readonly ConcurrentDictionary<TypeTuple, TypeAdapterConfigSettingsBase> _configurationCache = new ConcurrentDictionary<TypeTuple, TypeAdapterConfigSettingsBase>();

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

        internal static void UpsertConfigurationCache<TSource, TDestination>(
            TypeAdapterConfigSettings<TSource, TDestination> config)
        {
            var key = new TypeTuple(typeof (TSource), typeof (TDestination));
            _configurationCache[key] = config;
        }

        internal static void RemoveFromConfigurationCache<TSource, TDestination>()
        {
            var key = new TypeTuple(typeof(TSource), typeof(TDestination));
            TypeAdapterConfigSettingsBase obj;
            _configurationCache.TryRemove(key, out obj);
        }

        internal static bool ExistsInConfigurationCache(Type sourceType, Type destinationType)
        {
            var key = new TypeTuple(sourceType, destinationType);

            return _configurationCache.ContainsKey(key);
        }

        internal static TypeAdapterConfigSettingsBase GetConfigurationCache(Type sourceType, Type destinationType)
        {
            var key = new TypeTuple(sourceType, destinationType);
            TypeAdapterConfigSettingsBase returnValue;

            _configurationCache.TryGetValue(key, out returnValue);

            return returnValue;
        }

        internal static void ClearConfigurationCache()
        {
            _configurationCache.Clear();
        }

    }
    
    public class TypeAdapterConfig<TSource, TDestination>
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
            TypeAdapterConfig.UpsertConfigurationCache(_configSettings);
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

        public void Compile()
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

        public TypeAdapterConfig<TSource, TDestination> Resolve<TDestinationMember, TSourceMember>(
            Expression<Func<TDestination, TDestinationMember>> member,
            Expression<Func<TSource, TSourceMember>> source, Expression<Func<TSource, bool>> shouldMap = null)
        {
            if (source == null)
                return this;

            var memberExp = member.Body as MemberExpression;

            if (memberExp == null)
            {
                var ubody = (UnaryExpression)member.Body;
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

        public TypeAdapterConfig<TSource, TDestination> Map<TDestinationMember>(
            Expression<Func<TDestination, TDestinationMember>> member,
            Expression<Func<TSource, TDestinationMember>> source, Expression<Func<TSource, bool>> shouldMap = null)
        {
            _projection.MapFrom(member, source);

            return Resolve(member, source, shouldMap);
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

        //public TypeAdapterConfig<TSource, TDestination> MaxDepth(int maxDepth)
        //{
        //    _configSettings.MaxDepth = maxDepth;

        //    _projection.MaxDepth(maxDepth);

        //    TypeAdapterConfig.GlobalSettings.EnableMaxDepth = true;

        //    return this;
        //}
        public TypeAdapterConfig<TSource, TDestination> PreserveReference(bool preserveReference)
        {
            _configSettings.PreserveReference = preserveReference;
            return this;
        }  

        public TransformsCollection DestinationTransforms
        {
            get
            {
                return _configSettings?.DestinationTransforms;
            }
        }

        private static TypeAdapterConfigSettings<TSource, TDestination> DeriveConfigSettings()
        {
            TypeAdapterConfigSettings<TSource, TDestination> configSettings = null;

            //See if we can convert inherited config settings.
            Type destType = typeof(TDestination);
            Type sourceType = typeof(TSource).BaseType;
            bool matchFound = false;

            while (destType != null && destType != typeof(object))
            {
                while (sourceType != null && sourceType != typeof(object))
                {
                    var baseConfigSettings = TypeAdapterConfig.GetConfigurationCache(sourceType, destType);
                    if (baseConfigSettings != null)
                    {
                        configSettings = new TypeAdapterConfigSettings<TSource, TDestination>
                        {
                            //MaxDepth = baseConfigSettings.MaxDepth,
                            PreserveReference = baseConfigSettings.PreserveReference,
                            IgnoreNullValues = baseConfigSettings.IgnoreNullValues,
                            SameInstanceForSameType = baseConfigSettings.SameInstanceForSameType
                        };

                        configSettings.IgnoreMembers.AddRange(baseConfigSettings.IgnoreMembers);
                        
                        configSettings.DestinationTransforms.Upsert(baseConfigSettings.DestinationTransforms.Transforms);

                        foreach (var baseResolver in baseConfigSettings.Resolvers)
                        {
                            var convertedResolver = new InvokerModel
                            {
                                MemberName = baseResolver.MemberName,
                                Invoker = baseResolver.Invoker,
                                Condition = baseResolver.Condition,
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

            var baseConfigSettings = TypeAdapterConfig.GetConfigurationCache(_configSettings.InheritedSourceType, _configSettings.InheritedDestinationType);
            if (baseConfigSettings != null)
            {
                if (_configSettings.IgnoreNullValues == null)
                    _configSettings.IgnoreNullValues = baseConfigSettings.IgnoreNullValues;
                //if (_configSettings.MaxDepth == null)
                //    _configSettings.MaxDepth = baseConfigSettings.MaxDepth;
                if (_configSettings.PreserveReference == null)
                    _configSettings.PreserveReference = baseConfigSettings.PreserveReference;
                if (_configSettings.SameInstanceForSameType == null)
                    _configSettings.SameInstanceForSameType = baseConfigSettings.SameInstanceForSameType;

                foreach (var ignoreMember in baseConfigSettings.IgnoreMembers)
                {
                    if(!_configSettings.IgnoreMembers.Contains(ignoreMember)
                        && _configSettings.Resolvers.All(x => x.MemberName != ignoreMember))
                        _configSettings.IgnoreMembers.Add(ignoreMember);
                }

                _configSettings.DestinationTransforms.TryAdd(baseConfigSettings.DestinationTransforms.Transforms);

                foreach (var baseResolver in baseConfigSettings.Resolvers)
                {
                    string memberName = baseResolver.MemberName;

                    if (_configSettings.Resolvers.All(x => x.MemberName != memberName))
                    {
                        var convertedResolver = new InvokerModel
                        {
                            MemberName = memberName,
                            Invoker = baseResolver.Invoker,
                            Condition = baseResolver.Condition,
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
}
