using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Mapster.Models
{
    internal class FieldModel : IMemberModel
    {
        private readonly FieldInfo _fieldInfo;
        public FieldModel(FieldInfo fieldInfo)
        {
            _fieldInfo = fieldInfo;
        }

        public Type Type => _fieldInfo.FieldType;
        public string Name => _fieldInfo.Name;
        public object Info => _fieldInfo;
        public AccessModifier SetterModifier
        {
            get { return _fieldInfo.IsInitOnly ? AccessModifier.None : AccessModifier.Public; }
        }

        public Expression GetExpression(Expression source)
        {
            return Expression.Field(source, _fieldInfo);
        }
        public IEnumerable<object> GetCustomAttributes(bool inherit)
        {
            return _fieldInfo.GetCustomAttributes(inherit);
        }
    }
}
