using System;
using System.Linq.Expressions;
using Mapster.Tool.Tests;
using Mapster.Tool.Tests.Mappers;

namespace Mapster.Tool.Tests.Mappers
{
    public partial class UserMapper : IUserMapper
    {
        public Expression<Func<_User, _UserDto>> UserProjection => p1 => new _UserDto()
        {
            Id = p1.Id,
            Name = p1.Name
        };
        public _UserDto MapTo(_User p2)
        {
            return p2 == null ? null : new _UserDto()
            {
                Id = p2.Id,
                Name = p2.Name
            };
        }
        public _UserDto MapTo(_User p3, _UserDto p4)
        {
            if (p3 == null)
            {
                return null;
            }
            _UserDto result = p4 ?? new _UserDto();
            
            typeof(_UserDto).GetProperty("Id").SetValue(result, (object)p3.Id);
            typeof(_UserDto).GetProperty("Name").SetValue(result, p3.Name);
            return result;
            
        }
    }
}