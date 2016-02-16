using System;
using System.Linq.Expressions;
using System.Reflection;
using Mapster.Utils;

namespace Mapster.Models
{
    internal class ParameterModel : IMemberModel
    {
        private readonly ParameterInfo _parameterInfo;
        public ParameterModel(ParameterInfo parameterInfo)
        {
            _parameterInfo = parameterInfo;
        }

        public Type Type => _parameterInfo.ParameterType;
        public string Name => _parameterInfo.Name.ToProperCase();
        public object Info => _parameterInfo;
        public bool HasSetter => false;

        public Expression GetExpression(Expression source)
        {
            return Expression.Variable(this.Type);
        }

        public object[] GetCustomAttributes(bool inherit)
        {
            return _parameterInfo.GetCustomAttributes(inherit);
        }
    }
}
