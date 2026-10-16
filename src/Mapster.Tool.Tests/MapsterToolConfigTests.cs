using FluentAssertions;

namespace Mapster.Tool.Tests
{
    public class MapsterToolConfigTests : TestBase
    {
        [Fact]
        public void PrintFullTypeName_option_True_GenerateCodeWithGlobalNamespacePreffix()
        {
            var mappersWithGlobal = new List<string>();
            var mapperConfig = ConfigHelpers.optMappers;

            mapperConfig.PrintFullTypeName = true;
            Generators.GenerateMappers(mapperConfig, mappersWithGlobal);

            CompileValidator.TryCompile(mappersWithGlobal[0], out string Errors).Should().BeTrue();
            mappersWithGlobal[0].Contains("global::").Should().BeTrue();
        }

        [Fact]
        public void PrintFullTypeName_option_False_NotGenerateCodeWithGlobalNamespacePreffix()
        {
            var mappersWithGlobal = new List<string>();
            var mapperConfig = ConfigHelpers.optMappers;

            mapperConfig.PrintFullTypeName = false;
            Generators.GenerateMappers(mapperConfig, mappersWithGlobal);

            mappersWithGlobal[0].Contains("global::").Should().BeFalse();
        }
    }
}
