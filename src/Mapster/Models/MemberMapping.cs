using System.Collections.Generic;
using System.Linq.Expressions;

namespace Mapster.Models
{
    internal class MemberMapping
    {
        public Expression Getter;
        public IMemberModelEx DestinationMember;
        public IgnoreDictionary.IgnoreItem Ignore;
        public List<InvokerModel> NextResolvers;
        public IgnoreDictionary NextIgnore;
        public ParameterExpression Source;
        public ParameterExpression? Destination;
        public bool UseDestinationValue;

        public bool HasSettings()
        {
            return NextResolvers.Count > 0 || NextIgnore.Count > 0;
        }
    }
}