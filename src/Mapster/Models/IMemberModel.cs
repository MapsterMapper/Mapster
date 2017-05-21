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
        AccessModifier AccessModifier { get; }

        Expression GetExpression(Expression source);
        Expression SetExpression(Expression source, Expression value);
        IEnumerable<object> GetCustomAttributes(bool inherit);
    }
}
