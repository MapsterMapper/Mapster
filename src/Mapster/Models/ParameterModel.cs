using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Mapster.Utils;

namespace Mapster.Models
{
    public class ParameterModel : IMemberModelEx
    {
        private readonly ParameterInfo _parameterInfo;
        public ParameterModel(ParameterInfo parameterInfo)
        {
            _parameterInfo = parameterInfo;
        }

        public Type Type => _parameterInfo.ParameterType;
        public string Name => _parameterInfo.Name.ToPascalCase();
        public object Info => _parameterInfo;
        public AccessModifier SetterModifier => AccessModifier.Public;
        public AccessModifier AccessModifier => AccessModifier.Public;

        public Expression GetExpression(Expression source)
        {
            return Expression.Variable(this.Type, _parameterInfo.Name);
        }
        public Expression SetExpression(Expression source, Expression value)
        {
            return Expression.Assign(GetExpression(source), value);
        }
        public IEnumerable<object> GetCustomAttributes(bool inherit)
        {
            return _parameterInfo.GetCustomAttributes(inherit);
        }
        public IEnumerable<CustomAttributeData> GetCustomAttributesData()
        {
            return _parameterInfo.GetCustomAttributesData();
        }
    }
}
