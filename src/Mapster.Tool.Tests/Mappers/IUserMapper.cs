using System.Linq.Expressions;

namespace Mapster.Tool.Tests.Mappers;

[Mapper]
public interface IUserMapper
{
    /// <summary>Gets the user projection expression.</summary>
    Expression<Func<_User, _UserDto>> UserProjection { get; }

    /// <summary>Maps a user to a DTO.</summary>
    _UserDto MapTo(_User user);

    /// <summary>Maps a user onto an existing DTO.</summary>
    _UserDto MapTo(_User user, _UserDto userDto);
}