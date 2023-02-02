using System.Reflection;

namespace Mapster.Utils;

public static class TypeAdapterConfigExtensions
{
    public static void ScanInheritedTypes(this TypeAdapterConfig config, Assembly assembly)
    {
        InterfaceDynamicMapper dynamicMapper = new(config, assembly);
        dynamicMapper.ApplyMappingFromAssembly();
    }
}