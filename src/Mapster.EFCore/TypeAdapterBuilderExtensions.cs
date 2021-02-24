using Mapster.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Mapster.EFCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace Mapster
{
    public static class TypeAdapterBuilderExtensions
    {
        public static TypeAdapterBuilder<TSource> EntityFromContext<TSource>(this TypeAdapterBuilder<TSource> builder, DbContext context)
        {
            const string dbKey = "Mapster.EFCore.db";
            return builder
                .AddParameters(dbKey, context)
                .ForkConfig(config =>
                {
                    foreach (var entityType in context.Model.GetEntityTypes())
                    {
                        var type = entityType.ClrType;
                        var keys = entityType.FindPrimaryKey().Properties.Select(p => p.Name).ToArray();
                        var settings = config.When((srcType, destType, mapType) => destType == type);
                        settings.Settings.ConstructUsingFactory = arg =>
                        {
                            //var db = (DbContext)MapContext.Current.Parameters[DB_KEY];
                            //var set = db.Set<T>();
                            //return set.Find(src.id) ?? new T();
                            var src = Expression.Parameter(arg.SourceType);
                            var dest = arg.MapType == MapType.MapToTarget
                                ? Expression.Parameter(arg.DestinationType)
                                : null;
                            var db = Expression.Variable(typeof(DbContext), "db");
                            var set = Expression.Variable(typeof(DbSet<>).MakeGenericType(arg.DestinationType), "set");

                            var current = typeof(MapContext).GetProperty(nameof(MapContext.Current), BindingFlags.Static | BindingFlags.Public);
                            var indexer = typeof(Dictionary<string, object>).GetProperties().First(item => item.GetIndexParameters().Length > 0);
                            var dbAssign = Expression.Assign(
                                db,
                                Expression.Convert(
                                    Expression.Property(
                                        Expression.Property(
                                            Expression.Property(null, current!),
                                            nameof(MapContext.Parameters)),
                                        indexer,
                                        Expression.Constant(dbKey)),
                                    typeof(DbContext)));

                            var setMethod = (from method in typeof(DbContext).GetMethods()
                                             where method.Name == nameof(DbContext.Set) &&
                                                   method.IsGenericMethod
                                             select method).First().MakeGenericMethod(arg.DestinationType);
                            var setAssign = Expression.Assign(set, Expression.Call(db, setMethod));

                            var getters = keys.Select(key => arg.DestinationType.GetProperty(key))
                                .Select(prop => new PropertyModel(prop!))
                                .Select(model => arg.Settings.ValueAccessingStrategies
                                    .Select(s => s(src, model, arg))
                                    .FirstOrDefault(exp => exp != null))
                                .Where(exp => exp != null)
                                .Select(exp => Expression.Convert(exp, typeof(object)))
                                .ToArray();
                            if (getters.Length != keys.Length)
                                throw new InvalidOperationException($"Cannot get key for sourceType={arg.SourceType.Name}, destinationType={arg.DestinationType.Name}");
                            Expression find = Expression.Call(set, nameof(DbContext.Find), null,
                                Expression.NewArrayInit(typeof(object), getters));
                            if (arg.MapType == MapType.MapToTarget)
                                find = Expression.Coalesce(find, dest!);
                            var ret = Expression.Coalesce(
                                find,
                                Expression.New(arg.DestinationType));
                            return Expression.Lambda(
                                Expression.Block(new[] { db, set }, dbAssign, setAssign, ret),
                                arg.MapType == MapType.MapToTarget ? new[] { src, dest } : new[] { src });
                        };
                    }
                }, context.GetType().FullName);
        }

        public static IQueryable<TDestination> ProjectToType<TDestination>(this IAdapterBuilder<IQueryable> source)
        {
            var queryable = source.Source.ProjectToType<TDestination>(source.Config);
            if (!source.HasParameter || source.Parameters.All(it => it.Key.StartsWith("Mapster.")))
                return new MapsterQueryable<TDestination>(queryable, source);

            var call = (MethodCallExpression) queryable.Expression;
            var project = call.Arguments[1];
            var mapContext = typeof(MapContext);
            var current = mapContext.GetProperty(nameof(MapContext.Current), BindingFlags.Public | BindingFlags.Static);
            var properties = mapContext.GetProperty(nameof(MapContext.Parameters), BindingFlags.Public | BindingFlags.Instance);
            var item = typeof(Dictionary<string, object>)
                .GetProperty("Item", BindingFlags.Public | BindingFlags.Instance)!
                .GetMethod;

            var map = new Dictionary<Expression, Expression>();
            foreach (var (key, value) in source.Parameters)
            {
                if (key.StartsWith("Mapster."))
                    continue;
                var currentEx = Expression.Property(null, current!);
                var propertiesEx = Expression.Property(currentEx, properties!);
                var itemEx = Expression.Call(propertiesEx, item!, Expression.Constant(key));
                
                map.Add(itemEx, Parameterize(value).Body);
            }

            var replaced = new ExpressionReplacer(map).Visit(project);
            var methodCallExpression = Expression.Call(call.Method, call.Arguments[0], replaced!);
            var replacedQueryable = queryable.Provider.CreateQuery<TDestination>(methodCallExpression);
            return new MapsterQueryable<TDestination>(replacedQueryable, source);
        }

        private static Expression<Func<object>> Parameterize(object value)
        {
            return () => value;
        }
    }

    internal class ExpressionReplacer : ExpressionVisitor
    {
        private readonly Dictionary<Expression, Expression> _map;

        public ExpressionReplacer(Dictionary<Expression, Expression> map)
        {
            _map = map;
        }

        public override Expression Visit(Expression node)
        {
            foreach (var (key, value) in _map)
            {
                if (ExpressionEqualityComparer.Instance.Equals(node, key))
                    return value;
            }

            return base.Visit(node);
        }
    }
}
