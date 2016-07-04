using Mapster.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Mapster.Adapters;
using Mapster.Utils;

namespace Mapster
{
    public class TypeAdapterConfig
    {
        public static List<TypeAdapterRule> RulesTemplate { get; } = CreateRuleTemplate();
        public static List<Func<Expression, IMemberModel, CompileArgument, Expression>> ValueAccessingStrategiesTemplate { get; } = ValueAccessingStrategy.GetDefaultStrategies();

        private static TypeAdapterConfig _globalSettings;

        public static TypeAdapterConfig GlobalSettings
        {
            get { return _globalSettings ?? (_globalSettings = new TypeAdapterConfig()); }
        }

        private static List<TypeAdapterRule> CreateRuleTemplate()
        {
            return new List<TypeAdapterRule>
            {
                new PrimitiveAdapter().CreateRule(),
                new RecordTypeAdapter().CreateRule(),
                new ClassAdapter().CreateRule(),
                new DictionaryAdapter().CreateRule(),
                new CollectionAdapter().CreateRule(),

                //dictionary accessor
                new TypeAdapterRule
                {
                    Priority = (srcType, destType, mapType) => srcType.GetDictionaryType()?.GetGenericArguments()[0] == typeof(string) ? -149 : (int?)null,
                    Settings = new TypeAdapterSettings
                    {
                        ValueAccessingStrategies = new[] { ValueAccessingStrategy.Dictionary }.ToList(),
                    }
                }
            };
        }

        public bool RequireDestinationMemberSource { get; set; }
        public bool RequireExplicitMapping { get; set; }
        public bool AllowImplicitDestinationInheritance { get; set; }

        public List<TypeAdapterRule> Rules { get; protected set; }
        public TypeAdapterSetter Default { get; protected set; }
        public Dictionary<TypeTuple, TypeAdapterRule> RuleMap { get; protected set; } = new Dictionary<TypeTuple, TypeAdapterRule>();

        public TypeAdapterConfig()
        {
            this.Rules = RulesTemplate.ToList();
            var settings = new TypeAdapterSettings
            {
                ValueAccessingStrategies = ValueAccessingStrategiesTemplate.ToList(),
                NameMatchingStrategy = NameMatchingStrategy.Exact,
            };
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

        public TypeAdapterSetter<TSource, TDestination> ForType<TSource, TDestination>()
        {
            var key = new TypeTuple(typeof(TSource), typeof(TDestination));
            var settings = GetSettings(key);
            return new TypeAdapterSetter<TSource, TDestination>(settings, this);
        }

        public TypeAdapterSetter<TDestination> ForDestinationType<TDestination>()
        {
            var key = new TypeTuple(typeof(void), typeof(TDestination));
            var settings = GetSettings(key);
            return new TypeAdapterSetter<TDestination>(settings, this);
        }

        private TypeAdapterSettings GetSettings(TypeTuple key)
        {
            TypeAdapterRule rule;
            if (!this.RuleMap.TryGetValue(key, out rule))
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
                    var score2 = GetSubclassDistance(sourceType, key.Source, true);
                    if (score2 == null)
                        return null;
                    return score1.Value + score2.Value;
                },
                Settings = new TypeAdapterSettings
                {
                    DestinationType = key.Destination,
                },
            };
        }

        private static TypeAdapterRule CreateDestinationTypeRule(TypeTuple key)
        {
            return new TypeAdapterRule
            {
                Priority = (sourceType, destinationType, mapType) => GetSubclassDistance(destinationType, key.Destination, true),
                Settings = new TypeAdapterSettings
                {
                    DestinationType = key.Destination,
                },
            };
        }

        private static int? GetSubclassDistance(Type type1, Type type2, bool allowInheritance)
        {
            if (type1 == type2)
                return 50;
            if (!allowInheritance)
                return null;

            if (type2.GetTypeInfo().IsInterface)
            {
                return type2.GetTypeInfo().IsAssignableFrom(type1.GetTypeInfo())
                    ? (int?)25
                    : null;
            }

            int score = 50;
            while (type1 != null && type1 != type2)
            {
                score--;
                type1 = type1.GetTypeInfo().BaseType;
            }
            return type1 == null ? null : (int?)score;
        }

        private readonly Hashtable _mapDict = new Hashtable();

        internal Func<TSource, TDestination> GetMapFunction<TSource, TDestination>()
        {
            var key = new TypeTuple(typeof(TSource), typeof(TDestination));
            object del = _mapDict[key] ?? AddToHash(_mapDict, key, CreateMapFunction);

            return (Func<TSource, TDestination>)del;
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

        internal Delegate GetMapFunction(Type sourceType, Type destinationType)
        {
            var key = new TypeTuple(sourceType, destinationType);
            object del = _mapDict[key] ?? AddToHash(_mapDict, key, CreateMapFunction);

            return (Delegate)del;
        }

        private readonly Hashtable _mapToTargetDict = new Hashtable();

        internal Func<TSource, TDestination, TDestination> GetMapToTargetFunction<TSource, TDestination>()
        {
            var key = new TypeTuple(typeof(TSource), typeof(TDestination));
            object del = _mapToTargetDict[key] ?? AddToHash(_mapToTargetDict, key, CreateMapToTargetFunction);

            return (Func<TSource, TDestination, TDestination>)del;
        }

        internal Delegate GetMapToTargetFunction(Type sourceType, Type destinationType)
        {
            var key = new TypeTuple(sourceType, destinationType);
            object del = _mapToTargetDict[key] ?? AddToHash(_mapToTargetDict, key, CreateMapToTargetFunction);

            return (Delegate)del;
        }

        private readonly Hashtable _projectionDict = new Hashtable();

        internal Expression<Func<TSource, TDestination>> GetProjectionExpression<TSource, TDestination>()
        {
            var key = new TypeTuple(typeof(TSource), typeof(TDestination));
            object del = _projectionDict[key] ?? AddToHash(_projectionDict, key, CreateProjectionCallExpression);

            return (Expression<Func<TSource, TDestination>>)((UnaryExpression)((MethodCallExpression)del).Arguments[1]).Operand;
        }

        internal MethodCallExpression GetProjectionCallExpression(Type sourceType, Type destinationType)
        {
            var key = new TypeTuple(sourceType, destinationType);
            object del = _projectionDict[key] ?? AddToHash(_projectionDict, key, CreateProjectionCallExpression);

            return (MethodCallExpression)del;
        }

        private Delegate CreateMapFunction(TypeTuple tuple)
        {
            var context = new CompileContext(this);
            context.Running.Add(tuple);
            try
            {
                var result = CreateMapExpression(tuple.Source, tuple.Destination, MapType.Map, context);
                var compiled = result.Compile();
                if (this == GlobalSettings)
                {
                    var field = typeof(TypeAdapter<,>).MakeGenericType(tuple.Source, tuple.Destination).GetField("Map");
                    field.SetValue(null, compiled);
                }
                return compiled;
            }
            finally
            {
                context.Running.Remove(tuple);
            }
        }

        private Delegate CreateMapToTargetFunction(TypeTuple tuple)
        {
            var context = new CompileContext(this);
            context.Running.Add(tuple);
            try
            {
                var result = CreateMapExpression(tuple.Source, tuple.Destination, MapType.MapToTarget, context);
                return result.Compile();
            }
            finally
            {
                context.Running.Remove(tuple);
            }
        }

        private MethodCallExpression CreateProjectionCallExpression(TypeTuple tuple)
        {
            var context = new CompileContext(this);
            context.Running.Add(tuple);
            try
            {
                var lambda = CreateMapExpression(tuple.Source, tuple.Destination, MapType.Projection, context);
                var source = Expression.Parameter(typeof(IQueryable<>).MakeGenericType(tuple.Source));
                var methodInfo = (from method in typeof(Queryable).GetMethods()
                                  where method.Name == "Select"
                                  let p = method.GetParameters()[1]
                                  where p.ParameterType.GetGenericArguments()[0].GetGenericTypeDefinition() == typeof(Func<,>)
                                  select method).First().MakeGenericMethod(tuple.Source, tuple.Destination);
                return Expression.Call(methodInfo, source, Expression.Quote(lambda));
            }
            finally
            {
                context.Running.Remove(tuple);
            }
        }

        private LambdaExpression CreateMapExpression(Type sourceType, Type destinationType, MapType mapType, CompileContext context)
        {
            var setting = GetMergedSettings(sourceType, destinationType, mapType);
            var fn = mapType == MapType.MapToTarget
                ? setting.ConverterToTargetFactory
                : setting.ConverterFactory;
            if (fn == null)
            {
                if (mapType == MapType.InlineMap)
                    return null;
                else
                    throw new InvalidOperationException(
                        $"ConverterFactory is not found for the following mapping: TSource: {sourceType} TDestination: {destinationType}");
            }

            var arg = new CompileArgument
            {
                SourceType = sourceType,
                DestinationType = destinationType,
                MapType = mapType,
                Context = context,
                Settings = setting,
            };
            return fn(arg);
        }

        internal LambdaExpression CreateInlineMapExpression(Type sourceType, Type destinationType, MapType mapType, CompileContext context)
        {
            var tuple = new TypeTuple(sourceType, destinationType);
            if (context.Running.Contains(tuple))
            {
                if (mapType == MapType.Projection)
                    throw new InvalidOperationException(
                        $"Projection does not support circular reference: TSource: {sourceType} TDestination: {destinationType}");
                return CreateInvokeExpression(sourceType, destinationType);
            }

            context.Running.Add(tuple);
            try
            {
                var exp = CreateMapExpression(sourceType, destinationType, mapType == MapType.Projection ? MapType.Projection : MapType.InlineMap, context);
                if (exp != null)
                {
                    var detector = new BlockExpressionDetector();
                    detector.Visit(exp);
                    if (detector.IsBlockExpression)
                        exp = null;
                }
                return exp ?? CreateInvokeExpression(sourceType, destinationType);
            }
            finally
            {
                context.Running.Remove(tuple);
            }
        }

        private LambdaExpression CreateInvokeExpression(Type sourceType, Type destinationType)
        {
            Expression invoker;
            if (this == GlobalSettings)
            {
                var field = typeof(TypeAdapter<,>).MakeGenericType(sourceType, destinationType).GetField("Map");
                invoker = Expression.Field(null, field);
            }
            else
            {
                var method = (from m in typeof(TypeAdapterConfig).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
                              where m.Name == "GetMapFunction"
                              select m).First().MakeGenericMethod(sourceType, destinationType);
                invoker = Expression.Call(Expression.Constant(this), method);
            }
            var p = Expression.Parameter(sourceType);
            var invoke = Expression.Call(invoker, "Invoke", null, p);
            return Expression.Lambda(invoke, p);
        }

        internal TypeAdapterSettings GetMergedSettings(Type sourceType, Type destinationType, MapType mapType)
        {
            var settings = (from rule in this.Rules.Reverse<TypeAdapterRule>()
                            let priority = rule.Priority(sourceType, destinationType, mapType)
                            where priority != null
                            orderby priority.Value descending
                            select rule.Settings).ToList();
            var result = new TypeAdapterSettings
            {
                NoInherit = settings.FirstOrDefault(s => s.NoInherit.HasValue)?.NoInherit
            };
            foreach (var setting in settings)
            {
                result.Apply(setting);
            }
            return result;
        }

        public void Compile()
        {
            var keys = RuleMap.Keys.ToList();
            foreach (var key in keys)
            {
                _mapDict[key] = CreateMapFunction(key);
                _mapToTargetDict[key] = CreateMapToTargetFunction(key);
            }
        }

        public void Compile(Type sourceType, Type destinationType)
        {
            var tuple = new TypeTuple(sourceType, destinationType);
            _mapDict[tuple] = CreateMapFunction(tuple);
            _mapToTargetDict[tuple] = CreateMapToTargetFunction(tuple);
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
            TypeAdapterRule rule;
            if (this.RuleMap.TryGetValue(key, out rule))
            {
                this.RuleMap.Remove(key);
                this.Rules.Remove(rule);
            }
            _mapDict.Remove(key);
            _mapToTargetDict.Remove(key);
            _projectionDict.Remove(key);
        }

        private static TypeAdapterConfig _cloneConfig;

        public TypeAdapterConfig Clone()
        {
            if (_cloneConfig == null)
            {
                _cloneConfig = new TypeAdapterConfig();
                _cloneConfig.Default.Settings.PreserveReference = true;
            }
            var fn = _cloneConfig.GetMapFunction<TypeAdapterConfig, TypeAdapterConfig>();
            return fn(this);
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

    public class TypeAdapterRule
    {
        public Func<Type, Type, MapType, int?> Priority;
        public TypeAdapterSettings Settings;
    }
}