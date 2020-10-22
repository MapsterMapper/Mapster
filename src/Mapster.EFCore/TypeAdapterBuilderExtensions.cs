using Mapster.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Mapster.EFCore;
using Microsoft.EntityFrameworkCore;

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
                                            Expression.Property(null, current),
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
                                .Select(prop => new PropertyModel(prop))
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
                                find = Expression.Coalesce(find, dest);
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
            return new MapsterQueryable<TDestination>(queryable, source);
        }
    }
}
