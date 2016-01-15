using Mapster.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Mapster
{
    public class TypeAdapterConfig
    {
        public static TypeAdapterConfig GlobalSettings = new TypeAdapterConfig();
        public static List<TypeAdapterRule> RulesTemplate = CreateRuleTemplate();
        private static List<TypeAdapterRule> CreateRuleTemplate()
        {
            return new List<TypeAdapterRule>();
        }

        public bool RequireDestinationMemberSource;
        public bool RequireExplicitMapping;
        public bool AllowImplicitDestinationInheritance;

        public readonly List<TypeAdapterRule> Rules;
        public readonly TypeAdapterSetter Default;
        internal readonly Dictionary<TypeTuple, TypeAdapterSettings> Dict = new Dictionary<TypeTuple, TypeAdapterSettings>();
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

        public TypeAdapterSetter When(Func<Type, Type, MapType, int?> priority)
        {
            var rule = new TypeAdapterRule
            {
                Priority = priority,
                Settings = new TypeAdapterSettings(),
            };
            this.Rules.Add(rule);
            return new TypeAdapterSetter(rule.Settings, this);
        }

        public TypeAdapterSetter<TSource, TDestination> ForType<TSource, TDestination>()
        {
            var rule = new TypeAdapterRule
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
                Settings = new TypeAdapterSettings(),
            };
            this.Rules.Add(rule);
            this.Dict[new TypeTuple(typeof(TSource), typeof(TDestination))] = rule.Settings;
            return new TypeAdapterSetter<TSource, TDestination>(rule.Settings, this);
        }

        private static int? GetSubclassDistance(Type type1, Type type2, bool allowInheritance)
        {
            if (type1 == type2)
                return 100;
            if (!allowInheritance)
                return null;

            if (type2.IsInterface)
            {
                return type2.IsAssignableFrom(type1)
                    ? (int?)50
                    : null;
            }

            int score = 100;
            while (type1 != null && type1 != type2)
            {
                score--;
                type1 = type1.BaseType;
            }
            return type1 == null ? null : (int?)score;
        }

        private ConcurrentDictionary<TypeTuple, Delegate> _mapDict = new ConcurrentDictionary<TypeTuple, Delegate>();
        public Func<TSource, TDestination> GetMapFunction<TSource, TDestination>()
        {
            return (Func<TSource, TDestination>)GetMapFunction(typeof(TSource), typeof(TDestination));
        }
        public Delegate GetMapFunction(Type sourceType, Type destinationType)
        {
            return _mapDict.GetOrAdd(new TypeTuple(sourceType, destinationType), CreateMapFunction);
        }

        private ConcurrentDictionary<TypeTuple, Delegate> _mapToTargetDict = new ConcurrentDictionary<TypeTuple, Delegate>();
        public Func<TSource, TDestination, TDestination> GetMapToTargetFunction<TSource, TDestination>()
        {
            return (Func<TSource, TDestination, TDestination>)GetMapToTargetFunction(typeof(TSource), typeof(TDestination));
        }
        public Delegate GetMapToTargetFunction(Type sourceType, Type destinationType)
        {
            return _mapToTargetDict.GetOrAdd(new TypeTuple(sourceType, destinationType), CreateMapToTargetFunction);
        }

        private ConcurrentDictionary<TypeTuple, LambdaExpression> _projectionDict = new ConcurrentDictionary<TypeTuple, LambdaExpression>();
        public Expression<Func<TSource, TDestination>> GetProjectionExpression<TSource, TDestination>()
        {
            return (Expression<Func<TSource, TDestination>>)GetProjectionExpression(typeof(TSource), typeof(TDestination));
        }
        public LambdaExpression GetProjectionExpression(Type sourceType, Type destinationType)
        {
            return _projectionDict.GetOrAdd(new TypeTuple(sourceType, destinationType), CreateProjectionExpression);
        }

        private Delegate CreateMapFunction(TypeTuple tuple)
        {
            var result = CreateMapExpression(tuple.Source, tuple.Destination, MapType.Map, new CompileContext(this));
            return result.Compile();
        }

        private Delegate CreateMapToTargetFunction(TypeTuple tuple)
        {
            var result = CreateMapExpression(tuple.Source, tuple.Destination, MapType.MapToTarget, new CompileContext(this));
            return result.Compile();
        }

        private LambdaExpression CreateProjectionExpression(TypeTuple tuple)
        {
            return CreateMapExpression(tuple.Source, tuple.Destination, MapType.Projection, new CompileContext(this));
        }

        private LambdaExpression CreateMapExpression(Type sourceType, Type destinationType, MapType mapType, CompileContext context)
        {
            var setting = GetMergedSettings(sourceType, destinationType, mapType);
            var fn = mapType == MapType.MapToTarget
                ? setting.ConverterToTargetFactory
                : setting.ConverterFactory;
            if (fn == null)
                throw new InvalidOperationException(
                    $"ConverterFactory is not found for the following mapping: TSource: {sourceType} TDestination: {destinationType}");

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

        internal Expression CreateInlineMapExpression(Type sourceType, Type destinationType, CompileContext context)
        {
            var tuple = new TypeTuple(sourceType, destinationType);
            if (context.Running.Contains(tuple))
            {
                var method = (from m in typeof(TypeAdapterConfig).GetMethods()
                              where m.Name == "GetMapFunction"
                              select m).First().MakeGenericMethod(sourceType, destinationType);
                var invoker = Expression.Call(Expression.Constant(this), method);
                var p = Expression.Parameter(sourceType);
                var invoke = Expression.Call(invoker, "Invoke", null, p);
                return Expression.Lambda(invoke, p);
            }

            context.Running.Add(tuple);
            try
            {
                return CreateMapExpression(sourceType, destinationType, MapType.InlineMap, context);
            }
            finally
            {
                context.Running.Remove(tuple);
            }
        }

        private TypeAdapterSettings GetMergedSettings(Type sourceType, Type destinationType, MapType mapType)
        {
            if (this.RequireExplicitMapping && mapType != MapType.InlineMap)
            {
                if (!this.Dict.ContainsKey(new TypeTuple(sourceType, destinationType)))
                    throw new InvalidOperationException(
                        $"Implicit mapping is not allowed (check GlobalSettings.RequireExplicitMapping) and no configuration exists for the following mapping: TSource: {sourceType} TDestination: {destinationType}");
            }

            var settings = from rule in this.Rules.Reverse<TypeAdapterRule>()
                           let priority = rule.Priority(sourceType, destinationType, mapType)
                           where priority != null
                           orderby priority.Value descending
                           select rule.Settings;
            var result = new TypeAdapterSettings();
            foreach (var setting in settings)
            {
                result.Apply(setting);
            }
            return result;
        }
    }

    public static class TypeAdapterConfig<TSource, TDestination>
    {
        public static TypeAdapterSetter<TSource, TDestination> NewConfig()
        {
            return TypeAdapterConfig.GlobalSettings.ForType<TSource, TDestination>();
        }
    }

    public class TypeAdapterRule
    {
        public Func<Type, Type, MapType, int?> Priority;
        public TypeAdapterSettings Settings;
    }
}