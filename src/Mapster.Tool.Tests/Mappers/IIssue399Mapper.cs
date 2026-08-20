using System.Linq.Expressions;

namespace Mapster.Tool.Tests.Mappers;

[Mapper]
internal interface IIssue399Mapper
{
    Expression<Func<Issue399Source, Issue399Destination>> ProjectToDestination { get; }
    Issue399Destination Map(Issue399Source source);
}
