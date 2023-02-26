using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Mapster.Utils;

public static class TypeAdapterConfigExtensions
{
    public static void ScanInheritedTypes(this TypeAdapterConfig config, Assembly assembly)
    {
        var types = assembly.GetTypes()
            .Where(t =>
                t.GetInterfaces()
                    .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IMapFrom<>)))
            .ToList();
        InterfaceDynamicMapper dynamicMapper = new(config, types);
        dynamicMapper.ApplyMappingFromAssembly();
    }

    internal static void ScanInheritedTypes(this TypeAdapterConfig config, List<Type> types)
    {
        types = types.Where(t =>
                t.GetInterfaces()
                    .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IMapFrom<>)))
            .ToList();
        InterfaceDynamicMapper dynamicMapper = new(config, types);
        dynamicMapper.ApplyMappingFromAssembly();
    }
}