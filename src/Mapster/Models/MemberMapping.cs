using System.Collections.Generic;
using System.Linq.Expressions;

namespace Mapster.Models
{
    internal class MemberMapping
    {
        public Expression Getter;
        public IMemberModelEx DestinationMember;
        public LambdaExpression SetterCondition;
        public List<InvokerModel> Resolvers;
        public IgnoreIfDictionary IgnoreIfs;
        public ParameterExpression Source;
        public ParameterExpression Destination;

        public bool HasSettings()
        {
            return Resolvers.Count > 0 || IgnoreIfs.Count > 0;
        }
    }
}