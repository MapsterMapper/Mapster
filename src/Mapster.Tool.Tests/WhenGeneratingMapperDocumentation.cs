using FluentAssertions;

namespace Mapster.Tool.Tests
{
    public class WhenGeneratingMapperDocumentation : TestBase
    {
        [Fact]
        public void Interface_members_inherit_documentation()
        {
            var mappers = new List<string>();
            Generators.GenerateMappers(ConfigHelpers.optMappers, mappers);

            var userMapper = mappers.Single(x => x.Contains("class UserMapper"));
            CompileValidator.TryCompile(userMapper, out _).Should().BeTrue();

            var lines = userMapper.Split('\n').Select(x => x.Trim()).ToList();
            var members = lines
                .Select((line, index) => (line, index))
                .Where(x => x.line.StartsWith("public ") && (x.line.Contains(" UserProjection ") || x.line.Contains(" MapTo(")))
                .ToList();

            members.Should().HaveCount(3);
            foreach (var (line, index) in members)
                lines[index - 1].Should().Be("/// <inheritdoc />", line);
        }
    }
}
