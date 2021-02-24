using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Mapster.Adapters;
using Mapster.Models;
using Mapster.Utils;

namespace Mapster
{
    public class TypeAdapterConfig
    {
        public static List<TypeAdapterRule> RulesTemplate { get; } = CreateRuleTemplate();
        public static TypeAdapterConfig GlobalSettings { get; } = new TypeAdapterConfig();

        private static List<TypeAdapterRule> CreateRuleTemplate()
        {
            return new List<TypeAdapterRule>
            {
                new PrimitiveAdapter().CreateRule(),    //-200
                new ClassAdapter().CreateRule(),        //-150
                new RecordTypeAdapter().CreateRule(),   //-149
                new CollectionAdapter().CreateRule(),   //-125
                new DictionaryAdapter().CreateRule(),   //-124
                new ArrayAdapter().CreateRule(),        //-123
                new MultiDimensionalArrayAdapter().CreateRule(), //-122
                new ObjectAdapter().CreateRule(),       //-111
                new StringAdapter().CreateRule(),       //-110
                new EnumAdapter().CreateRule(),         //-109

                //fallback rules
                new TypeAdapterRule
                {
                    Priority = arg => -200,
                    Settings = new TypeAdapterSettings
                    {
                        //match exact name
                        NameMatchingStrategy = NameMatchingStrategy.Exact,
                        ShouldMapMember =
                        {
                            ShouldMapMember.IgnoreAdaptIgnore,      //ignore AdaptIgnore attribute
                            ShouldMapMember.AllowPublic,            //match public prop
                            ShouldMapMember.AllowAdaptMember,       //match AdaptMember attribute
                        },
                        GetMemberNames =
                        {
                            GetMemberName.AdaptMember,              //get name using AdaptMember attribute
                        },
                        UseDestinationValues =
                        {
                            UseDestinationValue.Attribute,
                        },
                        ValueAccessingStrategies =
                        {
                            ValueAccessingStrategy.CustomResolver,  //get value from Map
                            ValueAccessingStrategy.PropertyOrField, //get value from properties/fields
                            ValueAccessingStrategy.GetMethod,       //get value from get method
                            ValueAccessingStrategy.FlattenMember,   //get value from chain of properties
                        }
                    }
                },

                //dictionary accessor
                new TypeAdapterRule
                {
                    Priority = arg => arg.SourceType.GetDictionaryType()?.GetGenericArguments()[0] == typeof(string) ? DictionaryAdapter.DefaultScore : (int?)null,
                    Settings = new TypeAdapterSettings
                    {
                        ValueAccessingStrategies =
                        {
                            ValueAccessingStrategy.CustomResolverForDictionary,
                            ValueAccessingStrategy.Dictionary,
                        },
                    }
                }
            };
        }

        public bool RequireDestinationMemberSource { get; set; }
        public bool RequireExplicitMapping { get; set; }
        public bool AllowImplicitDestinationInheritance { get; set; }
        public bool AllowImplicitSourceInheritance { get; set; } = true;
        public bool SelfContainedCodeGeneration { get; set; }

        public Func<LambdaExpression, Delegate> Compiler { get; set; } = lambda => lambda.Compile();

        public List<TypeAdapterRule> Rules { get; internal set; }
        public TypeAdapterSetter Default { get; internal set; }
        public ConcurrentDictionary<TypeTuple, TypeAdapterRule> RuleMap { get; internal set; } = new ConcurrentDictionary<TypeTuple, TypeAdapterRule>();

        public TypeAdapterConfig()
        {
            this.Rules = RulesTemplate.ToList();
            var settings = new TypeAdapterSettings();
            this.Default = new TypeAdapterSetter(settings, this);
            this.Rules.Add(new TypeAdapterRule
            {
                Priority = arg => -100,
                Settings = settings,
            });
        }

        public TypeAdapterSetter When(Func<Type, Type, MapType, bool> canMap)
        {
            var rule = new TypeAdapterRule
            {
                Priority = arg => canMap(arg.SourceType, arg.DestinationType, arg.MapType) ? (int?)25 : null,
                Settings = new TypeAdapterSettings(),
            };
            this.Rules.LockAdd(rule);
            return new TypeAdapterSetter(rule.Settings, this);
        }

        public TypeAdapterSetter When(Func<PreCompileArgument, bool> canMap)
        {
            var rule = new TypeAdapterRule
            {
                Priority = arg => canMap(arg) ? (int?)25 : null,
                Settings = new TypeAdapterSettings(),
            };
            this.Rules.LockAdd(rule);
            return new TypeAdapterSetter(rule.Settings, this);
        }

        public TypeAdapterSetter<TSource, TDestination> NewConfig<TSource, TDestination>()
        {
            Remove(typeof(TSource), typeof(TDestination));
            return ForType<TSource, TDestination>();
        }

        public TypeAdapterSetter NewConfig(Type sourceType, Type destinationType)
        {
            Remove(sourceType, destinationType);
            return ForType(sourceType, destinationType);
        }

        public TypeAdapterSetter<TSource, TDestination> ForType<TSource, TDestination>()
        {
            var key = new TypeTuple(typeof(TSource), typeof(TDestination));
            var settings = GetSettings(key);
            return new TypeAdapterSetter<TSource, TDestination>(settings, this);
        }

        public TypeAdapterSetter ForType(Type sourceType, Type destinationType)
        {
            var key = new TypeTuple(sourceType, destinationType);
            var settings = GetSettings(key);
            return new TypeAdapterSetter(settings, this);
        }

        public TypeAdapterSetter<TDestination> ForDestinationType<TDestination>()
        {
            var key = new TypeTuple(typeof(void), typeof(TDestination));
            var settings = GetSettings(key);
            return new TypeAdapterSetter<TDestination>(settings, this);
        }

        private TypeAdapterSettings GetSettings(TypeTuple key)
        {
            var rule = this.RuleMap.GetOrAdd(key, types =>
            {
                var r = types.Source == typeof(void)
                    ? CreateDestinationTypeRule(types)
                    : CreateTypeTupleRule(types);
                this.Rules.LockAdd(r);
                return r;
            });
            return rule.Settings;
        }

        private TypeAdapterRule CreateTypeTupleRule(TypeTuple key)
        {
            return new TypeAdapterRule
            {
                Priority = arg =>
                {
                    var score1 = GetSubclassDistance(arg.DestinationType, key.Destination, this.AllowImplicitDestinationInheritance);
                    if (score1 == null)
                        return null;
                    var score2 = GetSubclassDistance(arg.SourceType, key.Source, this.AllowImplicitSourceInheritance);
                    if (score2 == null)
                        return null;
                    return score1.Value + score2.Value;
                },
                Settings = new TypeAdapterSettings(),
            };
        }

        private static TypeAdapterRule CreateDestinationTypeRule(TypeTuple key)
        {
            return new TypeAdapterRule
            {
                Priority = arg => GetSubclassDistance(arg.DestinationType, key.Destination, true),
                Settings = new TypeAdapterSettings(),
            };
        }

        private static int? GetSubclassDistance(Type type1, Type type2, bool allowInheritance)
        {
            if (type1 == type2)
                return 50;

            //generic type definition
            int score = 35;
            if (type2.GetTypeInfo().IsGenericTypeDefinition)
            {
                while (type1 != null && type1.GetTypeInfo().IsGenericType && type1.GetGenericTypeDefinition() != type2)
                {
                    score--;
                    type1 = type1.GetTypeInfo().BaseType;
                }
                return type1 != null && type1.GetTypeInfo().IsGenericType && type1.GetGenericTypeDefinition() == type2 
                    ? (int?)score
                    : null;
            }
            if (!allowInheritance)
                return null;

            if (!type2.GetTypeInfo().IsAssignableFrom(type1.GetTypeInfo()))
                return null;

            //interface
            if (type2.GetTypeInfo().IsInterface)
                return 25;

            //base type
            score = 50;
            while (type1 != null && type1 != type2)
            {
                score--;
                type1 = type1.GetTypeInfo().BaseType;
            }
            return score;
        }

        private T AddToHash<T>(ConcurrentDictionary<TypeTuple, T> hash, TypeTuple key, Func<TypeTuple, T> func)
        {
            return hash.GetOrAdd(key, types =>
            {
                var del = func(types);
                hash[types] = del;

                if (this.RuleMap.TryGetValue(types, out var rule))
                    rule.Settings.Compiled = true;
                return del;

            });
        }

        private readonly ConcurrentDictionary<TypeTuple, Delegate> _mapDict = new ConcurrentDictionary<TypeTuple, Delegate>();
        public Func<TSource, TDestination> GetMapFunction<TSource, TDestination>()
        {
            return (Func<TSource, TDestination>)GetMapFunction(typeof(TSource), typeof(TDestination));
        }
        internal Delegate GetMapFunction(Type sourceType, Type destinationType)
        {
            var key = new TypeTuple(sourceType, destinationType);
            if (!_mapDict.TryGetValue(key, out var del))
                del = AddToHash(_mapDict, key, tuple => Compiler(CreateMapExpression(tuple, MapType.Map)));
            return del;
        }

        private readonly ConcurrentDictionary<TypeTuple, Delegate> _mapToTargetDict = new ConcurrentDictionary<TypeTuple, Delegate>();
        public Func<TSource, TDestination, TDestination> GetMapToTargetFunction<TSource, TDestination>()
        {
            return (Func<TSource, TDestination, TDestination>)GetMapToTargetFunction(typeof(TSource), typeof(TDestination));
        }
        internal Delegate GetMapToTargetFunction(Type sourceType, Type destinationType)
        {
            var key = new TypeTuple(sourceType, destinationType);
            if (!_mapToTargetDict.TryGetValue(key, out var del)) 
                del = AddToHash(_mapToTargetDict, key, tuple => Compiler(CreateMapExpression(tuple, MapType.MapToTarget)));
            return del;
        }

        private readonly ConcurrentDictionary<TypeTuple, MethodCallExpression> _projectionDict = new ConcurrentDictionary<TypeTuple, MethodCallExpression>();
        internal Expression<Func<TSource, TDestination>> GetProjectionExpression<TSource, TDestination>()
        {
            var del = GetProjectionCallExpression(typeof(TSource), typeof(TDestination));

            return (Expression<Func<TSource, TDestination>>)((UnaryExpression)del.Arguments[1]).Operand;
        }
        internal MethodCallExpression GetProjectionCallExpression(Type sourceType, Type destinationType)
        {
            var key = new TypeTuple(sourceType, destinationType);
            if (!_projectionDict.TryGetValue(key, out var del))
                del = AddToHash(_projectionDict, key, CreateProjectionCallExpression);
            return del;
        }

        private readonly ConcurrentDictionary<TypeTuple, Delegate> _dynamicMapDict = new ConcurrentDictionary<TypeTuple, Delegate>();
        public Func<object, TDestination> GetDynamicMapFunction<TDestination>(Type sourceType)
        {
            var key = new TypeTuple(sourceType, typeof(TDestination));
            if (!_dynamicMapDict.TryGetValue(key, out var del)) 
                del = AddToHash(_dynamicMapDict, key, tuple => Compiler(CreateDynamicMapExpression(tuple)));
            return (Func<object, TDestination>)del;
        }

        private Expression CreateSelfExpression()
        {
            if (this == GlobalSettings)
                return Expression.Property(null, typeof(TypeAdapterConfig).GetProperty(nameof(GlobalSettings))!);
            else
                return Expression.Constant(this);
        }

        internal Expression CreateDynamicMapInvokeExpressionBody(Type destinationType, Expression p1)
        {
            var method = (from m in typeof(TypeAdapterConfig).GetMethods(BindingFlags.Instance | BindingFlags.Public)
                where m.Name == nameof(GetDynamicMapFunction)
                select m).First().MakeGenericMethod(destinationType);
            var getType = typeof(object).GetMethod(nameof(GetType));
            var invoker = Expression.Call(CreateSelfExpression(), method, Expression.Call(p1, getType!));
            return Expression.Call(invoker, "Invoke", null, p1);
        }

        public LambdaExpression CreateMapExpression(TypeTuple tuple, MapType mapType)
        {
            var context = new CompileContext(this);
            context.Running.Add(tuple);
            Action<TypeAdapterConfig>? fork = null;
            try
            {
                var arg = GetCompileArgument(tuple, mapType, context);
                fork = arg.Settings.Fork;
                if (fork != null)
                {
                    var cloned = this.Clone();
                    fork(cloned);
                    context.Configs.Push(cloned);
                    arg.Settings = cloned.GetMergedSettings(tuple, mapType);
                }
                return CreateMapExpression(arg);
            }
            finally
            {
                if (fork != null)
                    context.Configs.Pop();
                context.Running.Remove(tuple);
            }
        }

        private MethodCallExpression CreateProjectionCallExpression(TypeTuple tuple)
        {
            var lambda = CreateMapExpression(tuple, MapType.Projection);
            var source = Expression.Parameter(typeof(IQueryable<>).MakeGenericType(tuple.Source));
            var methodInfo = (from method in typeof(Queryable).GetMethods()
                              where method.Name == nameof(Queryable.Select)
                              let p = method.GetParameters()[1]
                              where p.ParameterType.GetGenericArguments()[0].GetGenericTypeDefinition() == typeof(Func<,>)
                              select method).First().MakeGenericMethod(tuple.Source, tuple.Destination);
            return Expression.Call(methodInfo, source, Expression.Quote(lambda));
        }

        private static LambdaExpression CreateMapExpression(CompileArgument arg)
        {
            var fn = arg.MapType == MapType.MapToTarget
                ? arg.Settings.ConverterToTargetFactory
                : arg.Settings.ConverterFactory;
            if (fn == null)
                throw new CompileException(arg, new InvalidOperationException("ConverterFactory is not found"));
            try
            {
                return fn(arg);
            }
            catch (Exception ex)
            {
                throw new CompileException(arg, ex);
            }
        }

        private LambdaExpression CreateDynamicMapExpression(TypeTuple tuple)
        {
            var lambda = CreateMapExpression(tuple, MapType.Map);
            var pNew = Expression.Parameter(typeof(object));
            var pOld = lambda.Parameters[0];
            var assign = ExpressionEx.Assign(pOld, pNew);
            return Expression.Lambda(
                Expression.Block(new[] { pOld }, assign, lambda.Body),
                pNew);
        }

        internal LambdaExpression CreateInlineMapExpression(Type sourceType, Type destinationType, MapType mapType, CompileContext context, MemberMapping? mapping = null)
        {
            var tuple = new TypeTuple(sourceType, destinationType);
            var subFunction = context.IsSubFunction();

            if (!subFunction)
            {
                if (context.Running.Contains(tuple))
                {
                    if (mapType == MapType.Projection)
                        throw new InvalidOperationException("Projection does not support circular reference, please use MaxDepth setting");
                    return CreateMapInvokeExpression(sourceType, destinationType, mapType);
                }
                context.Running.Add(tuple);
            }

            try
            {
                var arg = GetCompileArgument(tuple, mapType, context);
                if (mapping != null)
                {
                    arg.Settings.Resolvers.AddRange(mapping.NextResolvers);
                    arg.Settings.Ignore.Apply(mapping.NextIgnore);
                    arg.UseDestinationValue = mapping.UseDestinationValue;
                }

                return CreateMapExpression(arg);
            }
            finally
            {
                if (!subFunction)
                    context.Running.Remove(tuple);
            }
        }

        internal LambdaExpression CreateMapInvokeExpression(Type sourceType, Type destinationType, MapType mapType)
        {
            return mapType == MapType.MapToTarget
                ? CreateMapToTargetInvokeExpression(sourceType, destinationType)
                : CreateMapInvokeExpression(sourceType, destinationType);
        }

        private LambdaExpression CreateMapInvokeExpression(Type sourceType, Type destinationType)
        {
            var p = Expression.Parameter(sourceType);
            var invoke = CreateMapInvokeExpressionBody(sourceType, destinationType, p);
            return Expression.Lambda(invoke, p);
        }

        internal Expression CreateMapInvokeExpressionBody(Type sourceType, Type destinationType, Expression p)
        {
            if (this.RequireExplicitMapping)
            {
                var key = new TypeTuple(sourceType, destinationType);
                _mapDict[key] = Compiler(CreateMapExpression(key, MapType.Map));
            }
            Expression invoker;
            if (this == GlobalSettings)
            {
                var field = typeof(TypeAdapter<,>).MakeGenericType(sourceType, destinationType).GetField("Map");
                invoker = Expression.Field(null, field);
            }
            else
            {
                var method = (from m in typeof(TypeAdapterConfig).GetMethods(BindingFlags.Instance | BindingFlags.Public)
                    where m.Name == nameof(GetMapFunction)
                    select m).First().MakeGenericMethod(sourceType, destinationType);
                invoker = Expression.Call(CreateSelfExpression(), method);
            }
            return Expression.Call(invoker, "Invoke", null, p);
        }

        internal Expression CreateMapToTargetInvokeExpressionBody(Type sourceType, Type destinationType, Expression p1, Expression p2)
        {
            if (this.RequireExplicitMapping)
            {
                var key = new TypeTuple(sourceType, destinationType);
                _mapToTargetDict[key] = Compiler(CreateMapExpression(key, MapType.MapToTarget));
            }
            var method = (from m in typeof(TypeAdapterConfig).GetMethods(BindingFlags.Instance | BindingFlags.Public)
                where m.Name == nameof(GetMapToTargetFunction)
                select m).First().MakeGenericMethod(sourceType, destinationType);
            var invoker = Expression.Call(CreateSelfExpression(), method);
            return Expression.Call(invoker, "Invoke", null, p1, p2);
        }

        private LambdaExpression CreateMapToTargetInvokeExpression(Type sourceType, Type destinationType)
        {
            var p1 = Expression.Parameter(sourceType);
            var p2 = Expression.Parameter(destinationType);
            var invoke = CreateMapToTargetInvokeExpressionBody(sourceType, destinationType, p1, p2);
            return Expression.Lambda(invoke, p1, p2);
        }

        private IEnumerable<TypeAdapterRule> GetAttributeSettings(TypeTuple tuple, MapType mapType)
        {
            var rules1 = from type in tuple.Source.GetAllTypes()
                from o in type.GetTypeInfo().GetCustomAttributesData()
                where typeof(AdaptToAttribute).IsAssignableFrom(o.GetAttributeType())
                let attr = o.CreateCustomAttribute<AdaptToAttribute>()
                where attr != null && (attr.MapType & mapType) != 0
                where attr.Type == null || attr.Type == tuple.Destination
                where attr.Name == null || attr.Name.Replace("[name]", type.Name) == tuple.Destination.Name
                let distance = GetSubclassDistance(tuple.Source, type, true)
                select new TypeAdapterRule
                {
                    Priority = arg => distance + 50,
                    Settings = CreateSettings(attr)
                };
            if (tuple.Source == tuple.Destination)
                return rules1;
            var rules2 = from type in tuple.Destination.GetAllTypes()
                from o in type.GetTypeInfo().GetCustomAttributesData()
                where typeof(AdaptFromAttribute).IsAssignableFrom(o.GetAttributeType()) ||
                      typeof(AdaptTwoWaysAttribute).IsAssignableFrom(o.GetAttributeType())
                let attr = o.CreateCustomAttribute<BaseAdaptAttribute>()
                where attr != null && (attr.MapType & mapType) != 0
                where attr.Type == null || attr.Type == tuple.Source
                where attr.Name == null || attr.Name.Replace("[name]", type.Name) == tuple.Source.Name
                let distance = GetSubclassDistance(tuple.Destination, type, true)
                select new TypeAdapterRule
                {
                    Priority = arg => distance + 50,
                    Settings = CreateSettings(attr)
                };
            return rules1.Concat(rules2);
        }

        private TypeAdapterSettings CreateSettings(BaseAdaptAttribute attr)
        {
            var settings = new TypeAdapterSettings();
            var setter = new TypeAdapterSetter(settings, this);
            setter.ApplyAdaptAttribute(attr);
            return settings;
        }

        internal TypeAdapterSettings GetMergedSettings(TypeTuple tuple, MapType mapType)
        {
            var arg = new PreCompileArgument
            {
                SourceType = tuple.Source,
                DestinationType = tuple.Destination,
                MapType = mapType,
                ExplicitMapping = this.RuleMap.ContainsKey(tuple),
            };

            //auto add setting if there is attr setting
            var attrSettings = GetAttributeSettings(tuple, mapType).ToList();
            if (!arg.ExplicitMapping && attrSettings.Any(rule => rule.Priority(arg) == 100))
            {
                GetSettings(tuple);
                arg.ExplicitMapping = true;
            }

            var result = new TypeAdapterSettings();
            lock (this.Rules)
            {
                var rules = this.Rules.Reverse<TypeAdapterRule>().Concat(attrSettings);
                var settings = from rule in rules
                    let priority = rule.Priority(arg)
                    where priority != null
                    orderby priority.Value descending
                    select rule.Settings;
                foreach (var setting in settings)
                {
                    result.Apply(setting);
                }
            }

            //remove recursive include types
            if (mapType == MapType.MapToTarget)
                result.Includes.Remove(tuple);
            else
                result.Includes.RemoveAll(t => t.Source == tuple.Source);
            return result;
        }

        private CompileArgument GetCompileArgument(TypeTuple tuple, MapType mapType, CompileContext context)
        {
            var setting = GetMergedSettings(tuple, mapType);
            return new CompileArgument
            {
                SourceType = tuple.Source,
                DestinationType = tuple.Destination,
                ExplicitMapping = this.RuleMap.ContainsKey(tuple),
                MapType = mapType,
                Context = context,
                Settings = setting,
            };
        }

        public void Compile()
        {
            var keys = RuleMap.Keys.ToList();
            foreach (var key in keys)
            {
                if (key.Source == typeof(void))
                    continue;
                _mapDict[key] = Compiler(CreateMapExpression(key, MapType.Map));
                _mapToTargetDict[key] = Compiler(CreateMapExpression(key, MapType.MapToTarget));
            }
        }

        public void Compile(Type sourceType, Type destinationType)
        {
            var tuple = new TypeTuple(sourceType, destinationType);
            _mapDict[tuple] = Compiler(CreateMapExpression(tuple, MapType.Map));
            _mapToTargetDict[tuple] = Compiler(CreateMapExpression(tuple, MapType.MapToTarget));
            if (this == GlobalSettings)
            {
                var field = typeof(TypeAdapter<,>).MakeGenericType(sourceType, destinationType).GetField("Map");
                field!.SetValue(null, _mapDict[tuple]);
            }
        }

        public void CompileProjection()
        {
            var keys = RuleMap.Keys.ToList();
            foreach (var key in keys)
            {
                _projectionDict[key] = CreateProjectionCallExpression(key);
            }
        }

        public void CompileProjection(Type sourceType, Type destinationType)
        {
            var tuple = new TypeTuple(sourceType, destinationType);
            _projectionDict[tuple] = CreateProjectionCallExpression(tuple);
        }

        public IList<IRegister> Scan(params Assembly[] assemblies)
        {
            List<IRegister> registers = assemblies.Select(assembly => assembly.GetTypes()
                .Where(x => typeof(IRegister).GetTypeInfo().IsAssignableFrom(x.GetTypeInfo()) && x.GetTypeInfo().IsClass && !x.GetTypeInfo().IsAbstract))
                .SelectMany(registerTypes =>
                    registerTypes.Select(registerType => (IRegister)Activator.CreateInstance(registerType))).ToList();

            this.Apply(registers);
            return registers;
        }

        public void Apply(IEnumerable<Lazy<IRegister>> registers)
        {
            this.Apply(registers.Select(register => register.Value));
        }

        public void Apply(IEnumerable<IRegister> registers)
        {
            foreach (IRegister register in registers)
            {
                register.Register(this);
            }
        }

        public void Apply(params IRegister[] registers)
        {
            foreach (IRegister register in registers)
            {
                register.Register(this);
            }
        }

        internal void Clear()
        {
            var keys = RuleMap.Keys.ToList();
            foreach (var key in keys)
            {
                Remove(key);
            }
        }

        internal void Remove(Type sourceType, Type destinationType)
        {
            var key = new TypeTuple(sourceType, destinationType);
            Remove(key);
        }

        private void Remove(TypeTuple key)
        {
            if (this.RuleMap.TryRemove(key, out var rule))
                this.Rules.LockRemove(rule);
            _mapDict.TryRemove(key, out _);
            _mapToTargetDict.TryRemove(key, out _);
            _projectionDict.TryRemove(key, out _);
            _dynamicMapDict.TryRemove(key, out _);
        }

        private static readonly Lazy<TypeAdapterConfig> _cloneConfig = new Lazy<TypeAdapterConfig>(() =>
        {
            var config = new TypeAdapterConfig();
            config.Default.Settings.PreserveReference = true;
            config.ForType<TypeAdapterSettings, TypeAdapterSettings>()
                .MapWith(src => src.Clone(), true);
            return config;
        });
        public TypeAdapterConfig Clone()
        { 
            var fn = _cloneConfig.Value.GetMapFunction<TypeAdapterConfig, TypeAdapterConfig>();
            return fn(this);
        }

        private ConcurrentDictionary<string, TypeAdapterConfig>? _inlineConfigs;
        private ConcurrentDictionary<string, TypeAdapterConfig> InlineConfigs =>
            _inlineConfigs ??= new ConcurrentDictionary<string, TypeAdapterConfig>();
        public TypeAdapterConfig Fork(Action<TypeAdapterConfig> action,
#if !NET40
            [CallerFilePath]
#endif
            string key1 = "",
#if !NET40
            [CallerLineNumber]
#endif
            int key2 = 0)
        {
            var key = $"{key1}|{key2}";
            return InlineConfigs.GetOrAdd(key, _ =>
            {
                var cloned = this.Clone();
                action(cloned);
                return cloned;
            });
        }
    }

    public static class TypeAdapterConfig<TSource, TDestination>
    {
        public static TypeAdapterSetter<TSource, TDestination> NewConfig()
        {
            return TypeAdapterConfig.GlobalSettings.NewConfig<TSource, TDestination>();
        }

        public static TypeAdapterSetter<TSource, TDestination> ForType()
        {
            return TypeAdapterConfig.GlobalSettings.ForType<TSource, TDestination>();
        }

        public static void Clear()
        {
            TypeAdapterConfig.GlobalSettings.Remove(typeof(TSource), typeof(TDestination));
        }
    }
}