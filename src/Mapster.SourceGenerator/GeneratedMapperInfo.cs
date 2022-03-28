using System;
using System.Collections.Generic;
using System.Text;

namespace Mapster.SourceGenerator
{
    internal class GeneratedMapperInfo
    {
        public GeneratedTypeInfo ParentTypeInfo { get; set; }
        public string SourceText { get; set; }
        public string ForwardMappingMethodName { get; set; }
        public string BackwardMappingMethodName { get; set; }

    }
}
