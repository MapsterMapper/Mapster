using Mapster.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Mapster
{
    public class IgnoreDictionary : Dictionary<string, IgnoreDictionary.IgnoreItem>, IApplyable<IgnoreDictionary>
    {
        public struct IgnoreItem
        {
            public LambdaExpression Condition { get; set; }
            public bool IsChildPath { get; set; }
        }

        public void Apply(object other)
        {
            if (other is IgnoreDictionary collection)
                Apply(collection);
        }
        public void Apply(IgnoreDictionary other)
        {
            foreach (var member in other)
            {
                this.Merge(member.Key, member.Value);
            }
        }

        internal void Merge(string name, IgnoreDictionary.IgnoreItem src)
        {
            if (src.Condition != null && this.TryGetValue(name, out var item))
            {
                if (item.Condition == null)
                    return;

                var param = src.Condition.Parameters.ToArray();
                var body = item.IsChildPath ? item.Condition.Body : item.Condition.Apply(param[0], param[1]);
                this[name] = new IgnoreItem
                {
                    Condition = Expression.Lambda(Expression.OrElse(src.Condition.Body, body), param),
                    IsChildPath = src.IsChildPath
                };
            }
            else
                this[name] = src;

        }

        internal IgnoreDictionary Next(ParameterExpression source, ParameterExpression? destination, string destMemberName)
        {
            var result = new IgnoreDictionary();
            foreach (var member in this)
            {
                if (!member.Key.StartsWith(destMemberName + "."))
                    continue;
                var next = new IgnoreItem
                {
                    Condition = member.Value.IsChildPath || member.Value.Condition == null
                        ? member.Value.Condition
                        : Expression.Lambda(member.Value.Condition.Apply(source, destination), source, destination),
                    IsChildPath = true
                };
                result.Merge(member.Key.Substring(destMemberName.Length + 1), next);
            }

            return result;
        }
    }
}
