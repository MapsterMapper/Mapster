using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mapster
{
    public static class TypeAdapterExtensions
    {
        const string ASYNC_KEY = "Mapster.Async.tasks";

        internal static U GetValueOrDefault<T, U>(this IDictionary<T, U> dict, T key)
        {
            return dict.TryGetValue(key, out var value) ? value : default;
        }

        public static TypeAdapterSetter<TDestination> AfterMappingAsync<TDestination>(
            this TypeAdapterSetter<TDestination> setter, Func<TDestination, Task> action)
        {
            setter.AfterMapping(dest =>
            {
                var tasks = (List<Task>) MapContext.Current?.Parameters.GetValueOrDefault(ASYNC_KEY);
                if (tasks == null)
                    throw new InvalidOperationException("Mapping contains async function, please use BuildAdapter.AdaptToTypeAsync instead");
                var task = action(dest);
                tasks.Add(task);
            });
            return setter;
        }

        public static TypeAdapterSetter<TSource, TDestination> AfterMappingAsync<TSource, TDestination>(
            this TypeAdapterSetter<TSource, TDestination> setter, Func<TSource, TDestination, Task> action)
        {
            setter.AfterMapping((src, dest) =>
            {
                var tasks = (List<Task>) MapContext.Current?.Parameters.GetValueOrDefault(ASYNC_KEY);
                if (tasks == null)
                    throw new InvalidOperationException("Mapping contains async function, please use BuildAdapter.AdaptToTypeAsync instead");
                var task = action(src, dest);
                tasks.Add(task);
            });
            return setter;
        }

        public static async Task<TDestination> AdaptToTypeAsync<TDestination>(this IAdapterBuilder builder)
        {
            var tasks = new List<Task>();
            builder.Parameters[ASYNC_KEY] = tasks;
            var result = builder.AdaptToType<TDestination>();
            await Task.WhenAll(tasks);
            return result;
        }

        public static async Task<TDestination> AdaptToAsync<TDestination>(this IAdapterBuilder builder, TDestination destination)
        {
            var tasks = new List<Task>();
            builder.Parameters[ASYNC_KEY] = tasks;
            var result = builder.AdaptTo(destination);
            await Task.WhenAll(tasks);
            return result;
        }

    }
}
