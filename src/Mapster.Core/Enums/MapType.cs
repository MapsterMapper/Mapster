using System;

namespace Mapster
{
    [Flags]
    public enum MapType
    {
        Map = 1,
        MapToTarget = 2,
        Projection = 4,
        ApplyNullPropagation = 8,
        CtorParam = 16,
    }
}