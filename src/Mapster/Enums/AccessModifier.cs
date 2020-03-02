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
        ProtectedInternal = 6,
        NonPublic = 7,
        Public = 8,
    }
}