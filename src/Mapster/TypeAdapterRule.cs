using System;

namespace Mapster
{
    public class TypeAdapterRule
    {
        public Func<PreCompileArgument, int?> Priority { get; set; }
        public TypeAdapterSettings Settings { get; set; }
    }
}