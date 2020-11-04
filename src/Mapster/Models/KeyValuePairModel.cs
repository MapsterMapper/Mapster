using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Mapster.Models
{
    public class KeyValuePairModel : IMemberModelEx
    {
        readonly Func<Expression, Expression, Expression> _getFn;
        readonly Func<Expression, Expression, Expression, Expression> _setFn;
        public KeyValuePairModel(string name, Type type,
            Func<Expression, Expression, Expression> getFn, 
            Func<Expression, Expression, Expression, Expression> setFn)
        {
            Name = name;
            Type = type;
            _getFn = getFn;
            _setFn = setFn;
        }

        public Type Type { get; }

        public string Name { get; }

        public object? Info => null;

        public AccessModifier SetterModifier => AccessModifier.Public;

        public AccessModifier AccessModifier => AccessModifier.Public;

        public IEnumerable<object> GetCustomAttributes(bool inherit) => Enumerable.Empty<object>();

        public IEnumerable<CustomAttributeData> GetCustomAttributesData() =>
            Enumerable.Empty<CustomAttributeData>();

        public Expression GetExpression(Expression source)
        {
            return _getFn(source, Expression.Constant(this.Name));
        }
        public Expression SetExpression(Expression source, Expression value)
        {
            return _setFn(source, Expression.Constant(this.Name), value);
        }
    }
}
