using Mapster.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Mapster
{
    public static class TypeAdapterBuilderExtensions
    {
        public static TypeAdapterBuilder<TSource> CreateEntityFromContext<TSource>(this TypeAdapterBuilder<TSource> builder, IObjectContextAdapter context)
        {
            const string DB_KEY = "Mapster.EF6.db";
            return builder
                .AddParameters(DB_KEY, context)
                .ForkConfig(config =>
                {
                    var oc = context.ObjectContext;
                    var assmb = context.GetType().Assembly;
                    var entities = oc.MetadataWorkspace.GetItems<EntityType>(DataSpace.CSpace);
                    foreach (var entity in entities)
                    {
                        var type = assmb.GetType(entity.FullName);
                        var keys = entity.KeyMembers.Select(member => member.Name).ToArray();
                        var settings = config.When((srcType, destType, mapType) => destType == type);
                        settings.Settings.ConstructUsingFactory = arg =>
                        {
                            //var db = (DbContext)MapContext.Current.Parameters[DB_KEY];
                            //var set = db.Set<T>();
                            //return set.Find(src.id) ?? new T();
                            var src = Expression.Parameter(arg.SourceType);
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
                                        Expression.Constant(DB_KEY)),
                                    typeof(DbContext)));

                            var setMethod = (from method in typeof(DbContext).GetMethods()
                                             where method.Name == nameof(DbContext.Set) &&
                                                   method.IsGenericMethod
                                             select method).First().MakeGenericMethod(arg.DestinationType);
                            var setAssign = Expression.Call(db, setMethod);

                            var mergedSettings = config.GetMergedSettings(arg.SourceType, arg.DestinationType, arg.MapType);
                            var getters = keys.Select(key => arg.DestinationType.GetProperty(key))
                                .Select(prop => new PropertyModel(prop))
                                .Select(model => mergedSettings.ValueAccessingStrategies.Select(s => s(src, model, arg)).FirstOrDefault())
                                .Where(exp => exp != null)
                                .Select(exp => Expression.Convert(exp, typeof(object)))
                                .ToArray();
                            if (getters.Length != keys.Length)
                                throw new InvalidOperationException($"Cannot get key for sourceType={arg.SourceType.Name}, destinationType={arg.DestinationType.Name}");
                            var ret = Expression.Coalesce(
                                Expression.Call(set, nameof(DbSet.Find), null, Expression.NewArrayInit(typeof(object), getters)),
                                Expression.New(arg.DestinationType));
                            return Expression.Lambda(
                                Expression.Block(new[] { db, set }, dbAssign, setAssign, ret),
                                src);
                        };
                    }
                }, context.GetType().FullName);
        }
    }
}
