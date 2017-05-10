using System;

namespace Mapster
{
    public class TypeAdapterRule
    {
        public Func<Type, Type, MapType, int?> Priority;
        public TypeAdapterSettings Settings;
    }
}