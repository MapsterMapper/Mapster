using Mapster.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Mapster.Adapters;

namespace Mapster
{
    public class TypeAdapterConfig
    {
        public static readonly List<TypeAdapterRule> RulesTemplate = CreateRuleTemplate();
        public static readonly TypeAdapterConfig GlobalSettings = new TypeAdapterConfig();
        private static List<TypeAdapterRule> CreateRuleTemplate()
        {
            return new List<TypeAdapterRule>
            {
                new PrimitiveAdapter().CreateRule(),
                new ClassAdapter().CreateRule(),
                new CollectionAdapter().CreateRule(),
            };
        }

        public bool RequireDestinationMemberSource;
        public bool RequireExplicitMapping;
        public bool AllowImplicitDestinationInheritance;

        public readonly List<TypeAdapterRule> Rules;
        public readonly TypeAdapterSetter Default;
        internal readonly Dictionary<TypeTuple, TypeAdapterRule> Dict = new Dictionary<TypeTuple, TypeAdapterRule>();
        public TypeAdapterConfig()
        {
            this.Rules = RulesTemplate.ToList();
            this.Default = new TypeAdapterSetter(new TypeAdapterSettings(), this);
            this.Rules.Add(new TypeAdapterRule
            {
                Priority = (sourceType, destinationType, mapType) => -100,
                Settings = this.Default.Settings,
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

        public TypeAdapterSetter<TSource, TDestination> ForType<TSource, TDestination>()
        {
            var key = new TypeTuple(typeof (TSource), typeof (TDestination));
            TypeAdapterRule rule;
            if (!this.Dict.TryGetValue(key, out rule))
            {
                lock(this.Dict)
                {
                    if (!this.Dict.TryGetValue(key, out rule))
                    {
                        rule = new TypeAdapterRule
                        {
                            Priority = (sourceType, destinationType, mapType) =>
                            {
                                var score1 = GetSubclassDistance(destinationType, typeof(TDestination), this.AllowImplicitDestinationInheritance);
                                if (score1 == null)
                                    return null;
                                var score2 = GetSubclassDistance(sourceType, typeof(TSource), true);
                                if (score2 == null)
                                    return null;
                                return score1.Value + score2.Value;
                            },
                            Settings = new TypeAdapterSettings
                            {
                                DestinationType = typeof(TDestination)
                            },
                        };
                        this.Rules.Add(rule);
                        this.Dict.Add(key, rule);
                    }
                }
            }
            return new TypeAdapterSetter<TSource, TDestination>(rule.Settings, this);
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
            object del = _mapDict[key];
            if (del != null)
                return (Func<TSource, TDestination>)del;

            return (Func<TSource, TDestination>)AddToHash(_mapDict, key, CreateMapFunction);
        }

        private static object AddToHash(Hashtable hash, TypeTuple key, Func<TypeTuple, object> func)
        {
            lock (hash)
            {
                var del = hash[key];
                if (del != null)
                    return del;
                del = func(key);
                hash[key] = del;
                return del;
            }
        }
        internal Delegate GetMapFunction(Type sourceType, Type destinationType)
        {
            var key = new TypeTuple(sourceType, destinationType);
            object del = _mapDict[key];
            if (del != null)
                return (Delegate)del;

            return (Delegate)AddToHash(_mapDict, key, CreateMapFunction);
        }

        private readonly Hashtable _mapToTargetDict = new Hashtable();
        internal Func<TSource, TDestination, TDestination> GetMapToTargetFunction<TSource, TDestination>()
        {
            var key = new TypeTuple(typeof(TSource), typeof(TDestination));
            object del = _mapToTargetDict[key];
            if (del != null)
                return (Func<TSource, TDestination, TDestination>)del;

            return (Func<TSource, TDestination, TDestination>)AddToHash(_mapToTargetDict, key, CreateMapToTargetFunction);
        }
        internal Delegate GetMapToTargetFunction(Type sourceType, Type destinationType)
        {
            var key = new TypeTuple(sourceType, destinationType);
            object del = _mapToTargetDict[key];
            if (del != null)
                return (Delegate)del;

            return (Delegate)AddToHash(_mapToTargetDict, key, CreateMapToTargetFunction);
        }

        private readonly Hashtable _projectionDict = new Hashtable();
        internal Expression<Func<TSource, TDestination>> GetProjectionExpression<TSource, TDestination>()
        {
            var key = new TypeTuple(typeof(TSource), typeof(TDestination));
            object del = _projectionDict[key];
            if (del != null)
                return (Expression<Func<TSource, TDestination>>)del;

            return (Expression<Func<TSource, TDestination>>)AddToHash(_projectionDict, key, CreateProjectionExpression);
        }
        internal LambdaExpression GetProjectionExpression(Type sourceType, Type destinationType)
        {
            var key = new TypeTuple(sourceType, destinationType);
            object del = _projectionDict[key];
            if (del != null)
                return (LambdaExpression)del;

            return (LambdaExpression)AddToHash(_projectionDict, key, CreateProjectionExpression);
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
                    var field = typeof (TypeAdapter<,>).MakeGenericType(tuple.Source, tuple.Destination).GetField("Map");
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

        private LambdaExpression CreateProjectionExpression(TypeTuple tuple)
        {
            var context = new CompileContext(this);
            context.Running.Add(tuple);
            try
            {
                return CreateMapExpression(tuple.Source, tuple.Destination, MapType.Projection, context);
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
                return CreateInvokeExpression(sourceType, destinationType);

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
                var field = typeof (TypeAdapter<,>).MakeGenericType(sourceType, destinationType).GetField("Map");
                invoker = Expression.Field(null, field);
            }
            else
            {
                var method = (from m in typeof (TypeAdapterConfig).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
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
            if (this.RequireExplicitMapping && mapType != MapType.InlineMap)
            {
                if (!this.Dict.ContainsKey(new TypeTuple(sourceType, destinationType)))
                    throw new InvalidOperationException(
                        $"Implicit mapping is not allowed (check GlobalSettings.RequireExplicitMapping) and no configuration exists for the following mapping: TSource: {sourceType} TDestination: {destinationType}");
            }

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
            foreach (var kvp in Dict)
            {
                _mapDict[kvp.Key] = CreateMapFunction(kvp.Key);
                _mapToTargetDict[kvp.Key] = CreateMapToTargetFunction(kvp.Key);
                _projectionDict[kvp.Key] = CreateProjectionExpression(kvp.Key);
            }
        }

        public void Compile(Type sourceType, Type destinationType)
        {
            var tuple = new TypeTuple(sourceType, destinationType);
            _mapDict[tuple] = CreateMapFunction(tuple);
            _mapToTargetDict[tuple] = CreateMapToTargetFunction(tuple);
            _projectionDict[tuple] = CreateProjectionExpression(tuple);
        }

        internal void Clear(Type sourceType, Type destinationType)
        {
            var key = new TypeTuple(sourceType, destinationType);
            TypeAdapterRule rule;
            if (this.Dict.TryGetValue(key, out rule))
            {
                this.Dict.Remove(key);
                this.Rules.Remove(rule);
            }
            _mapDict.Remove(key);
            _mapToTargetDict.Remove(key);
            _projectionDict.Remove(key);
        }
    }

    public static class TypeAdapterConfig<TSource, TDestination>
    {
        public static TypeAdapterSetter<TSource, TDestination> NewConfig()
        {
            Clear();
            return TypeAdapterConfig.GlobalSettings.ForType<TSource, TDestination>();
        }

        public static void Clear()
        {
            TypeAdapterConfig.GlobalSettings.Clear(typeof(TSource), typeof(TDestination));
        }
    }

    public class TypeAdapterRule
    {
        public Func<Type, Type, MapType, int?> Priority;
        public TypeAdapterSettings Settings;
    }
}