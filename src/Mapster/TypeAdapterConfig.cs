using Mapster.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Mapster.Adapters;
using Mapster.Utils;
using System.Runtime.CompilerServices;

namespace Mapster
{
    public class TypeAdapterConfig
    {
        public static List<TypeAdapterRule> RulesTemplate { get; } = CreateRuleTemplate();
        public static List<Func<Expression, IMemberModel, CompileArgument, Expression>> ValueAccessingStrategiesTemplate { get; } = ValueAccessingStrategy.GetDefaultStrategies();

        private static TypeAdapterConfig _globalSettings;
        public static TypeAdapterConfig GlobalSettings => _globalSettings ?? (_globalSettings = new TypeAdapterConfig());

        private static List<TypeAdapterRule> CreateRuleTemplate()
        {
            return new List<TypeAdapterRule>
            {
                new PrimitiveAdapter().CreateRule(),    //-200
                new RecordTypeAdapter().CreateRule(),   //-151
                new ClassAdapter().CreateRule(),        //-150
                new DictionaryAdapter().CreateRule(),   //-149
                new CollectionAdapter().CreateRule(),   //-125

                //dictionary accessor
                new TypeAdapterRule
                {
                    Priority = (srcType, destType, mapType) => srcType.GetDictionaryType()?.GetGenericArguments()[0] == typeof(string) ? -149 : (int?)null,
                    Settings = new TypeAdapterSettings
                    {
                        ValueAccessingStrategies = { ValueAccessingStrategy.Dictionary },
                    }
                }
            };
        }

        public bool RequireDestinationMemberSource { get; set; }
        public bool RequireExplicitMapping { get; set; }
        public bool AllowImplicitDestinationInheritance { get; set; }
        public bool AllowImplicitSourceInheritance { get; set; } = true;
        public Func<LambdaExpression, Delegate> Compiler { get; set; } = lambda => lambda.Compile();

        public List<TypeAdapterRule> Rules { get; protected set; }
        public TypeAdapterSetter Default { get; protected set; }
        public Dictionary<TypeTuple, TypeAdapterRule> RuleMap { get; protected set; } = new Dictionary<TypeTuple, TypeAdapterRule>();

        public TypeAdapterConfig()
        {
            this.Rules = RulesTemplate.ToList();
            var settings = new TypeAdapterSettings
            {
                NameMatchingStrategy = NameMatchingStrategy.Exact,
                ShouldMapMember =
                {
                    ShouldMapMember.AllowPublic,
                    ShouldMapMember.IgnoreAdaptIgnore,
                    ShouldMapMember.AllowAdaptMember,
                },
                GetMemberNames =
                {
                    GetMemberName.AdaptMember,
                }
            };
            settings.ValueAccessingStrategies.AddRange(ValueAccessingStrategiesTemplate);
            this.Default = new TypeAdapterSetter(settings, this);
            this.Rules.Add(new TypeAdapterRule
            {
                Priority = (sourceType, destinationType, mapType) => -100,
                Settings = settings,
            });
        }

        public TypeAdapterSetter When(Func<Type, Type, MapType, bool> canMap)
        {
            var rule = new TypeAdapterRule
            {
                Priority = (srcType, destType, mapType) => canMap(srcType, destType, mapType) ? (int?)25 : null,
                Settings = new TypeAdapterSettings(),
            };
            this.Rules.Add(rule);
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
            if (!this.RuleMap.TryGetValue(key, out var rule))
            {
                lock (this.RuleMap)
                {
                    if (!this.RuleMap.TryGetValue(key, out rule))
                    {
                        rule = key.Source == typeof(void)
                            ? CreateDestinationTypeRule(key)
                            : CreateTypeTupleRule(key);
                        this.Rules.Add(rule);
                        this.RuleMap.Add(key, rule);
                    }
                }
            }
            return rule.Settings;
        }

        private TypeAdapterRule CreateTypeTupleRule(TypeTuple key)
        {
            return new TypeAdapterRule
            {
                Priority = (sourceType, destinationType, mapType) =>
                {
                    var score1 = GetSubclassDistance(destinationType, key.Destination, this.AllowImplicitDestinationInheritance);
                    if (score1 == null)
                        return null;
                    var score2 = GetSubclassDistance(sourceType, key.Source, this.AllowImplicitSourceInheritance);
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
                Priority = (sourceType, destinationType, mapType) => GetSubclassDistance(destinationType, key.Destination, true),
                Settings = new TypeAdapterSettings(),
            };
        }

        private static int? GetSubclassDistance(Type type1, Type type2, bool allowInheritance)
        {
            if (type1 == type2)
                return 50;
            if (!allowInheritance)
                return null;

            //generic type definition
            int score = 35;
            if (type2.GetTypeInfo().IsGenericTypeDefinition)
            {
                while (type1 != null && type1.GetGenericTypeDefinition() != type2)
                {
                    score--;
                    type1 = type1.GetTypeInfo().BaseType;
                }
                return type1 == null ? null : (int?) score;
            }

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

        private object AddToHash(Hashtable hash, TypeTuple key, Func<TypeTuple, object> func)
        {
            lock (hash)
            {
                var del = hash[key];
                if (del != null)
                    return del;

                del = func(key);
                hash[key] = del;

                var settings = GetSettings(key);
                settings.Compiled = true;
                return del;
            }
        }

        private readonly Hashtable _mapDict = new Hashtable();
        public Func<TSource, TDestination> GetMapFunction<TSource, TDestination>()
        {
            return (Func<TSource, TDestination>)GetMapFunction(typeof(TSource), typeof(TDestination));
        }
        internal Delegate GetMapFunction(Type sourceType, Type destinationType)
        {
            var key = new TypeTuple(sourceType, destinationType);
            object del = _mapDict[key] ?? AddToHash(_mapDict, key, tuple => Compiler(CreateMapExpression(tuple, MapType.Map)));

            return (Delegate)del;
        }

        private readonly Hashtable _mapToTargetDict = new Hashtable();
        public Func<TSource, TDestination, TDestination> GetMapToTargetFunction<TSource, TDestination>()
        {
            return (Func<TSource, TDestination, TDestination>)GetMapToTargetFunction(typeof(TSource), typeof(TDestination));
        }
        internal Delegate GetMapToTargetFunction(Type sourceType, Type destinationType)
        {
            var key = new TypeTuple(sourceType, destinationType);
            object del = _mapToTargetDict[key] ?? AddToHash(_mapToTargetDict, key, tuple => Compiler(CreateMapExpression(tuple, MapType.MapToTarget)));

            return (Delegate)del;
        }

        private readonly Hashtable _projectionDict = new Hashtable();
        internal Expression<Func<TSource, TDestination>> GetProjectionExpression<TSource, TDestination>()
        {
            var del = GetProjectionCallExpression(typeof(TSource), typeof(TDestination));

            return (Expression<Func<TSource, TDestination>>)((UnaryExpression)del.Arguments[1]).Operand;
        }
        internal MethodCallExpression GetProjectionCallExpression(Type sourceType, Type destinationType)
        {
            var key = new TypeTuple(sourceType, destinationType);
            object del = _projectionDict[key] ?? AddToHash(_projectionDict, key, CreateProjectionCallExpression);

            return (MethodCallExpression)del;
        }

        internal LambdaExpression CreateMapExpression(TypeTuple tuple, MapType mapType)
        {
            var context = new CompileContext(this);
            context.Running.Add(tuple);
            try
            {
                var arg = GetCompileArgument(tuple.Source, tuple.Destination, mapType, context);
                return CreateMapExpression(arg);
            }
            finally
            {
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
            {
                if (arg.MapType == MapType.InlineMap)
                    return null;
                else
                    throw new CompileException(arg, 
                        new InvalidOperationException("ConverterFactory is not found"));
            }
            try
            {
                return fn(arg);
            }
            catch (Exception ex)
            {
                throw new CompileException(arg, ex);
            }
        }

        internal LambdaExpression CreateInlineMapExpression(Type sourceType, Type destinationType, MapType parentMapType, CompileContext context)
        {
            var tuple = new TypeTuple(sourceType, destinationType);
            if (context.Running.Contains(tuple))
            {
                if (parentMapType == MapType.Projection)
                    throw new InvalidOperationException("Projection does not support circular reference");
                return CreateMapInvokeExpression(sourceType, destinationType);
            }

            context.Running.Add(tuple);
            try
            {
                var arg = GetCompileArgument(tuple.Source, tuple.Destination, parentMapType == MapType.Projection ? MapType.Projection : MapType.InlineMap, context);
                var exp = CreateMapExpression(arg);
                if (exp != null)
                {
                    var detector = new BlockExpressionDetector();
                    detector.Visit(exp);
                    if (detector.IsBlockExpression)
                        exp = null;
                }
                return exp ?? CreateMapInvokeExpression(sourceType, destinationType);
            }
            finally
            {
                context.Running.Remove(tuple);
            }
        }

        private LambdaExpression CreateMapInvokeExpression(Type sourceType, Type destinationType)
        {
            Expression invoker;
            if (this == GlobalSettings)
            {
                var field = typeof(TypeAdapter<,>).MakeGenericType(sourceType, destinationType).GetField("Map");
                invoker = Expression.Field(null, field);
            }
            else
            {
                var method = (from m in typeof(TypeAdapterConfig).GetMethods(BindingFlags.Instance | BindingFlags.Public)
                              where m.Name == nameof(TypeAdapterConfig.GetMapFunction)
                              select m).First().MakeGenericMethod(sourceType, destinationType);
                invoker = Expression.Call(Expression.Constant(this), method);
            }
            var p = Expression.Parameter(sourceType);
            var invoke = Expression.Call(invoker, "Invoke", null, p);
            return Expression.Lambda(invoke, p);
        }

        internal LambdaExpression CreateMapToTargetInvokeExpression(Type sourceType, Type destinationType)
        {
            var method = (from m in typeof(TypeAdapterConfig).GetMethods(BindingFlags.Instance | BindingFlags.Public)
                          where m.Name == nameof(TypeAdapterConfig.GetMapToTargetFunction)
                          select m).First().MakeGenericMethod(sourceType, destinationType);
            var invoker = Expression.Call(Expression.Constant(this), method);
            var p1 = Expression.Parameter(sourceType);
            var p2 = Expression.Parameter(destinationType);
            var invoke = Expression.Call(invoker, "Invoke", null, p1, p2);
            return Expression.Lambda(invoke, p1, p2);
        }

        internal TypeAdapterSettings GetMergedSettings(Type sourceType, Type destinationType, MapType mapType)
        {
            var settings = (from rule in this.Rules.Reverse<TypeAdapterRule>()
                            let priority = rule.Priority(sourceType, destinationType, mapType)
                            where priority != null
                            orderby priority.Value descending
                            select rule.Settings).ToList();
            var result = new TypeAdapterSettings();
            foreach (var setting in settings)
            {
                result.Apply(setting);
            }

            //remove recursive include types
            if (mapType == MapType.MapToTarget)
                result.Includes.Remove(new TypeTuple(sourceType, destinationType));
            else
                result.Includes.RemoveAll(tuple => tuple.Source == sourceType);
            return result;
        }

        CompileArgument GetCompileArgument(Type sourceType, Type destinationType, MapType mapType, CompileContext context)
        {
            var setting = GetMergedSettings(sourceType, destinationType, mapType);
            var arg = new CompileArgument
            {
                SourceType = sourceType,
                DestinationType = destinationType,
                MapType = mapType,
                Context = context,
                Settings = setting,
            };
            return arg;
        }

        public void Compile()
        {
            var keys = RuleMap.Keys.ToList();
            foreach (var key in keys)
            {
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
                field.SetValue(null, _mapDict[tuple]);
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
            if (this.RuleMap.TryGetValue(key, out var rule))
            {
                this.RuleMap.Remove(key);
                this.Rules.Remove(rule);
            }
            _mapDict.Remove(key);
            _mapToTargetDict.Remove(key);
            _projectionDict.Remove(key);
        }

        internal static TypeAdapterConfig _cloneConfig;
        public TypeAdapterConfig Clone()
        {
            if (_cloneConfig == null)
            {
                _cloneConfig = new TypeAdapterConfig();
                _cloneConfig.Default.Settings.PreserveReference = true;
                _cloneConfig.ForType<TypeAdapterSettings, TypeAdapterSettings>()
                    .MapWith(src => src.Clone(), true);
            }
            var fn = _cloneConfig.GetMapFunction<TypeAdapterConfig, TypeAdapterConfig>();
            return fn(this);
        }

        private Hashtable _inlineConfigs;
        public TypeAdapterConfig Fork(Action<TypeAdapterConfig> action,
#if !NET40
            [CallerFilePath]
#endif
            string key1 = null,
#if !NET40
            [CallerLineNumber]
#endif
            int key2 = 0)
        {
            if (_inlineConfigs == null)
                _inlineConfigs = new Hashtable();
            var key = key1 + '|' + key2;
            var config = _inlineConfigs[key];
            if (config != null)
                return (TypeAdapterConfig)config;

            lock(_inlineConfigs)
            {
                config = _inlineConfigs[key];
                if (config != null)
                    return (TypeAdapterConfig)config;

                var cloned = this.Clone();
                action(cloned);
                _inlineConfigs[key] = config = cloned;
            }
            return (TypeAdapterConfig)config;
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