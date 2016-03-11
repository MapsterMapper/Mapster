using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Mapster.Models
{
    internal class PropertyModel : IMemberModel
    {
        private readonly PropertyInfo _propertyInfo;
        public PropertyModel(PropertyInfo propertyInfo)
        {
            _propertyInfo = propertyInfo;
        }

        public Type Type => _propertyInfo.PropertyType;
        public string Name => _propertyInfo.Name;
        public object Info => _propertyInfo;

        public AccessModifier SetterModifier
        {
            get
            {
                var setter = _propertyInfo.GetSetMethod();
                if (setter == null)
                    return AccessModifier.None;

                if (setter.IsFamilyOrAssembly)
                    return AccessModifier.Protected | AccessModifier.Internal;
                if (setter.IsFamily)
                    return AccessModifier.Protected;
                if (setter.IsAssembly)
                    return AccessModifier.Internal;
                if (setter.IsPublic)
                    return AccessModifier.Public;
                return AccessModifier.Private;
            }
        }

        public Expression GetExpression(Expression source)
        {
            return Expression.Property(source, _propertyInfo);
        }
        public IEnumerable<object> GetCustomAttributes(bool inherit)
        {
            return _propertyInfo.GetCustomAttributes(inherit);
        }

    }
}
