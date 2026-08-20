using FluentAssertions;
using Mapster.Tool.Tests.Mappers;

namespace Mapster.Tool.Tests;

/// <summary>
/// Tests for https://github.com/MapsterMapper/Mapster/issues/399
/// </summary>
public class WhenMapperInterfaceUsesInternalTypes : TestBase
{
    [Fact]
    public void MapperInterfaceImplementationUsesInternalTypes()
    {
        IIssue399Mapper mapper = new Issue399Mapper();
        var source = new Issue399Source { Id = 1, Name = "Test" };

        var destination = mapper.Map(source);

        destination.Id.Should().Be(source.Id);
        destination.Name.Should().Be(source.Name);
    }
}

internal class Issue399Source
{
    public int Id { get; init; }
    public string? Name { get; init; }
}

internal class Issue399Destination
{
    public int Id { get; init; }
    public string? Name { get; init; }
}
