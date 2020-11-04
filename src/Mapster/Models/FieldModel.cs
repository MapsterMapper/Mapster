using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Mapster.Models
{
    public class FieldModel : IMemberModelEx
    {
        private readonly FieldInfo _fieldInfo;
        public FieldModel(FieldInfo fieldInfo)
        {
            _fieldInfo = fieldInfo;
        }

        public Type Type => _fieldInfo.FieldType;
        public string Name => _fieldInfo.Name;
        public object Info => _fieldInfo;
        public AccessModifier SetterModifier => _fieldInfo.IsInitOnly ? AccessModifier.None : _fieldInfo.GetAccessModifier();
        public AccessModifier AccessModifier => _fieldInfo.GetAccessModifier();

        public Expression GetExpression(Expression source)
        {
            return Expression.Field(source, _fieldInfo);
        }
        public Expression SetExpression(Expression source, Expression value)
        {
            return Expression.Assign(GetExpression(source), value);
        }
        public IEnumerable<object> GetCustomAttributes(bool inherit)
        {
            return _fieldInfo.GetCustomAttributes(inherit);
        }
        public IEnumerable<CustomAttributeData> GetCustomAttributesData()
        {
            return _fieldInfo.GetCustomAttributesData();
        }
    }
}
