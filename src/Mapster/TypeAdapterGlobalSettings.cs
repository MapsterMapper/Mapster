using System.Collections.Generic;

namespace Mapster
{
    public class TypeAdapterGlobalSettings
    {
        public readonly TransformsCollection DestinationTransforms = new TransformsCollection();

        public bool RequireDestinationMemberSource;

        public bool RequireExplicitMapping;

        public bool AllowImplicitDestinationInheritance;

        //public bool EnableMaxDepth;

        public bool PreserveReference;

        public readonly List<ITypeAdapter> CustomAdapters = new List<ITypeAdapter>();
    }
}