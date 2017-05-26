using System;

namespace Mapster
{
    public class TypeAdapterRule
    {
        public Func<PreCompileArgument, int?> Priority;
        public TypeAdapterSettings Settings;
    }
}