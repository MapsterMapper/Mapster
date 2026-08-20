using System;
using System.Linq.Expressions;
using Mapster.Tool.Tests;
using Mapster.Tool.Tests.Mappers;

namespace Mapster.Tool.Tests.Mappers
{
    internal partial class Issue399Mapper : IIssue399Mapper
    {
        public Expression<Func<Issue399Source, Issue399Destination>> ProjectToDestination => p1 => new Issue399Destination()
        {
            Id = p1.Id,
            Name = p1.Name
        };
        public Issue399Destination Map(Issue399Source p2)
        {
            return p2 == null ? null : new Issue399Destination()
            {
                Id = p2.Id,
                Name = p2.Name
            };
        }
    }
}