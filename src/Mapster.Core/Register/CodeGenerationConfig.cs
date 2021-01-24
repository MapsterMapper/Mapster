using System.Collections.Generic;

namespace Mapster
{
    public class CodeGenerationConfig
    {
        public List<AdaptAttributeBuilder> AdaptAttributeBuilders { get; } = new List<AdaptAttributeBuilder>();
        public List<GenerateMapperAttributeBuilder> GenerateMapperAttributeBuilders { get; } = new List<GenerateMapperAttributeBuilder>();
        public AdaptAttributeBuilder Default { get; } = new AdaptAttributeBuilder(new AdaptFromAttribute("void"));

        public AdaptAttributeBuilder AdaptTo(string name, MapType? mapType = null)
        {
            var builder = new AdaptAttributeBuilder(new AdaptToAttribute(name) {MapType = mapType ?? 0});
            AdaptAttributeBuilders.Add(builder);
            return builder;
        }

        public AdaptAttributeBuilder AdaptFrom(string name, MapType? mapType = null)
        {
            var builder = new AdaptAttributeBuilder(new AdaptFromAttribute(name) {MapType = mapType ?? 0});
            AdaptAttributeBuilders.Add(builder);
            return builder;
        }

        public AdaptAttributeBuilder AdaptTwoWays(string name, MapType? mapType = null)
        {
            var builder = new AdaptAttributeBuilder(new AdaptTwoWaysAttribute(name) {MapType = mapType ?? 0});
            AdaptAttributeBuilders.Add(builder);
            return builder;
        }

        public GenerateMapperAttributeBuilder GenerateMapper(string name)
        {
            var builder = new GenerateMapperAttributeBuilder(new GenerateMapperAttribute());
            GenerateMapperAttributeBuilders.Add(builder);
            return builder;
        }
    }
}