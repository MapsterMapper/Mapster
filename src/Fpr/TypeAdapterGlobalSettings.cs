
using System;
using System.Collections.Generic;

namespace Fpr
{
    public class TypeAdapterGlobalSettings
    {
        public readonly TransformsCollection DestinationTransforms = new TransformsCollection();

        public bool RequireDestinationMemberSource;

        public bool RequireExplicitMapping;

        public bool AllowImplicitDestinationInheritance;

        public readonly HashSet<Type> PrimitiveType = new HashSet<Type>(); 
    }
}