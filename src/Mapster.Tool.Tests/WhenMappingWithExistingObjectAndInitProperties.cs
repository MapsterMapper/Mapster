using System.Reflection;
using FluentAssertions;
using Mapster.Tool.Tests.Mappers;

namespace Mapster.Tool.Tests;

/// <summary>
/// Tests for https://github.com/MapsterMapper/Mapster/issues/536
/// </summary>
public class WhenMappingWithExistingObjectAndInitProperties : TestBase
{
    [Fact]
    public void MapWithReflection()
    {
        TypeAdapterConfig.GlobalSettings
            .Scan(Assembly.GetExecutingAssembly());
        
        var userMapper = GetMappingInterface<IUserMapper>();
        var expected = "Aref";
        var user = new _User { Name = expected, Id = 1 };
        var dto = new _UserDto();
        userMapper.MapTo(user, dto);
        dto.Name.Should().Be(expected);
    }
}

public class UserMappingRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<_User, _UserDto>()
            .MapToConstructor(true)
            .ConstructUsing(s => new _UserDto());
    }
}

public class _User
{
    public int Id { get; init; }
    public string Name { get; init; }
}

public class _UserDto
{
    public int Id { get; init; }
    public string Name { get; init; }
}