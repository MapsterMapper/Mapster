using Mapster.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Mapster
{
    public class IgnoreIfDictionary : Dictionary<string, LambdaExpression>, IApplyable<IgnoreIfDictionary>
    {
        public void Apply(object other)
        {
            if (other is IgnoreIfDictionary collection)
                Apply(collection);
        }
        public void Apply(IgnoreIfDictionary other)
        {
            foreach (var member in other)
            {
                this.Merge(member.Key, member.Value);
            }
        }

        internal void Merge(string name, LambdaExpression condition)
        {
            if (condition != null && this.TryGetValue(name, out var lambda))
            {
                if (lambda == null)
                    return;

                var param = lambda.Parameters.ToArray();
                lambda = Expression.Lambda(Expression.OrElse(lambda.Body, condition.Apply(true, param[0], param[1])), param);
                this[name] = lambda;
            }
            else
                this[name] = condition;

        }
    }
}
