using System;
using System.Collections.Generic;

namespace Mapster
{
    public class TypeAdapterGlobalSettings
    {
        public readonly TransformsCollection DestinationTransforms = new TransformsCollection();

        public bool RequireDestinationMemberSource;

        public bool RequireExplicitMapping;

        public bool AllowImplicitDestinationInheritance;

        public readonly HashSet<Type> PrimitiveTypes = new HashSet<Type>(); 
    }
}