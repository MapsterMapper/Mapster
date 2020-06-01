using Mapster.Utils;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;

namespace Mapster
{
    public class IgnoreDictionary : ConcurrentDictionary<string, IgnoreDictionary.IgnoreItem>, IApplyable<IgnoreDictionary>
    {
        public readonly struct IgnoreItem
        {
            public IgnoreItem(LambdaExpression? condition, bool isChildPath)
            {
                Condition = condition;
                IsChildPath = isChildPath;
            }

            public LambdaExpression? Condition { get; }
            public bool IsChildPath { get; }
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

        internal void Merge(string name, in IgnoreItem src)
        {
            if (src.Condition != null && this.TryGetValue(name, out var item))
            {
                if (item.Condition == null)
                    return;

                var param = src.Condition.Parameters.ToArray();
                var body = item.IsChildPath ? item.Condition.Body : item.Condition.Apply(param[0], param[1]);
                var condition = Expression.Lambda(Expression.OrElse(src.Condition.Body, body), param);

                TryUpdate(name, new IgnoreItem(condition, src.IsChildPath), item);
            }
            else
                TryAdd(name, src);

        }

        internal IgnoreDictionary Next(ParameterExpression source, ParameterExpression? destination, string destMemberName)
        {
            var result = new IgnoreDictionary();
            foreach (var member in this)
            {
                if (!member.Key.StartsWith(destMemberName + "."))
                    continue;

                var condition = member.Value.IsChildPath || member.Value.Condition == null
                    ? member.Value.Condition
                    : Expression.Lambda(member.Value.Condition.Apply(source, destination), source, destination);

                var next = new IgnoreItem(condition, true);
                result.Merge(member.Key.Substring(destMemberName.Length + 1), next);
            }

            return result;
        }
    }
}
