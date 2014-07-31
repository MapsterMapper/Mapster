
namespace Fpr
{
    public class TypeAdapterGlobalSettings
    {
        public readonly TransformsCollection DestinationTransforms = new TransformsCollection();

        public bool RequireDestinationMemberSource;

        public bool RequireExplicitMapping;

        public bool AllowImplicitDestinationInheritance;

    }
}