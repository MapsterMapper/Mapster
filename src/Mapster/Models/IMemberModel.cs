using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Mapster.Models
{
    public interface IMemberModel
    {
        Type Type { get; }
        string Name { get; }
        object Info { get; }
        AccessModifier SetterModifier { get; }

        Expression GetExpression(Expression source);
        IEnumerable<object> GetCustomAttributes(bool inherit);
    }
}
