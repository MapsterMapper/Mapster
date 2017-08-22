using System.Linq.Expressions;

namespace Mapster.Models
{
    internal class MemberMapping
    {
        public Expression Getter;
        public IMemberModelEx DestinationMember;
        public LambdaExpression SetterCondition;
    }
}