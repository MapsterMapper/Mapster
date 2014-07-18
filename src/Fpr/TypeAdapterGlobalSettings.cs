
namespace Fpr
{
    public class TypeAdapterGlobalSettings
    {
        private readonly TransformsCollection _destinationTransforms = new TransformsCollection();

        public bool RequireDestinationMemberSource { get; set; }

        public bool RequireExplicitMapping { get; set; }

        public bool ApplyMappingToDescendents { get; set; }

        public TransformsCollection DestinationTransforms
        {
            get { return _destinationTransforms; }
        }
    }
}