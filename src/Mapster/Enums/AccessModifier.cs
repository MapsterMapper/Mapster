using System;

namespace Mapster
{
    [Flags]
    public enum AccessModifier
    {
        None = 0,
        Private = 1,
        Protected = 2,
        Internal = 4,
        Public = 8,
    }
}