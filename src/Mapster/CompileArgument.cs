using System;

namespace Mapster
{
    public class CompileArgument
    {
        public Type SourceType;
        public Type DestinationType;
        public MapType MapType;
        public TypeAdapterSettings Settings;
        public CompileContext Context;
    }
}