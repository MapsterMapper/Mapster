using System;
using System.Collections.Generic;
using System.Reflection;

namespace Mapster.Models
{
    public interface IMemberModel
    {
        Type Type { get; }
        string Name { get; }
        object? Info { get; }
        AccessModifier SetterModifier { get; }
        AccessModifier AccessModifier { get; }

        IEnumerable<object> GetCustomAttributes(bool inherit);
        IEnumerable<CustomAttributeData> GetCustomAttributesData();
    }
}
