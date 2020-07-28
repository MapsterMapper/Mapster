using System;

namespace Mapster
{
    public class PreCompileArgument
    {
        public Type SourceType { get; set; }
        public Type DestinationType { get; set; }
        public MapType MapType { get; set; }
        public bool ExplicitMapping { get; set; }
    }
}
