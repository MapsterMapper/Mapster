using System;
using System.Collections.Generic;
using MapsterMapper;

namespace Mapster
{
    public static class TypeAdapterExtensions
    {
        internal static U GetValueOrDefault<T, U>(this IDictionary<T, U> dict, T key)
        {
            return dict.TryGetValue(key, out var value) ? value : default;
        }

        public static TService GetService<TService>(this MapContext context)
        {
            var sp = (IServiceProvider) context?.Parameters.GetValueOrDefault(ServiceMapper.DI_KEY);
            if (sp == null)
                throw new InvalidOperationException("Mapping must be called using ServiceAdapter");
            return (TService)sp.GetService(typeof(TService));
        }
    }
}
