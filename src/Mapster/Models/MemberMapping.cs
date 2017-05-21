using System.Linq.Expressions;

namespace Mapster.Models
{
    internal class MemberMapping
    {
        public Expression Getter;
        public IMemberModel DestinationMember;
        public LambdaExpression SetterCondition;
    }
}