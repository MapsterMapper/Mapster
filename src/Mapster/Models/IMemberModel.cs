using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Mapster.Models
{
    internal interface IMemberModel
    {
        Type Type { get; }
        string Name { get; }
        object Info { get; }
        bool HasSetter { get; }

        Expression GetExpression(Expression source);
        IEnumerable<object> GetCustomAttributes(bool inherit);
    }
}
