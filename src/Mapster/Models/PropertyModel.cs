using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Mapster.Models
{
    public class PropertyModel : IMemberModelEx
    {
        private readonly PropertyInfo _propertyInfo;
        public PropertyModel(PropertyInfo propertyInfo)
        {
            _propertyInfo = propertyInfo;
        }

        public Type Type => _propertyInfo.PropertyType;
        public virtual string Name => _propertyInfo.Name;
        public object Info => _propertyInfo;

        public AccessModifier SetterModifier
        {
            get
            {
                var setter = _propertyInfo.GetSetMethod(true);
                return setter?.GetAccessModifier() ?? AccessModifier.None;
            }
        }
        public AccessModifier AccessModifier
        {
            get
            {
                var getter = _propertyInfo.GetGetMethod(true);
                return getter?.GetAccessModifier() ?? AccessModifier.None;
            }
        }

        public virtual Expression GetExpression(Expression source)
        {
            return Expression.Property(source, _propertyInfo);
        }
        public Expression SetExpression(Expression source, Expression value)
        {
            return Expression.Assign(GetExpression(source), value);
        }
        public IEnumerable<object> GetCustomAttributes(bool inherit)
        {
            return _propertyInfo.GetCustomAttributes(inherit);
        }
        public IEnumerable<CustomAttributeData> GetCustomAttributesData()
        {
            return _propertyInfo.GetCustomAttributesData();
        }
    }
}
