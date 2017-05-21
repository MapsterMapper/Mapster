using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Mapster.Models
{
    internal class PropertyModel : IMemberModel
    {
        protected readonly PropertyInfo _propertyInfo;
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
                var setter = _propertyInfo.GetSetMethod();
                return setter == null ? AccessModifier.None : setter.GetAccessModifier();
            }
        }
        public AccessModifier AccessModifier
        {
            get
            {
                var getter = _propertyInfo.GetGetMethod();
                return getter == null ? AccessModifier.None : getter.GetAccessModifier();
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

    }
}
