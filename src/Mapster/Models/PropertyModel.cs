using System;
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
        public bool HasSetter => _propertyInfo.GetSetMethod() != null;

        public Expression GetExpression(Expression source)
        {
            return Expression.Property(source, _propertyInfo);
        }
        public object[] GetCustomAttributes(bool inherit)
        {
            return _propertyInfo.GetCustomAttributes(inherit);
        }

    }
}
