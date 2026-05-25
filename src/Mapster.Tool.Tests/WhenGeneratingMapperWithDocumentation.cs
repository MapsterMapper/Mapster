using FluentAssertions;

namespace Mapster.Tool.Tests;

public class WhenGeneratingMapperWithDocumentation
{
    [Fact]
    public void GeneratedMapperShouldIncludeInheritDocForDocumentedMembers()
    {
        var generatedMapperPath = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Mappers", "UserMapper.cs")
        );

        File.Exists(generatedMapperPath).Should().BeTrue();
        var content = File.ReadAllText(generatedMapperPath);
        content.Should().Contain("/// <inheritdoc />");
    }
}
