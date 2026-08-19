using Mapster.Utils;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Mapster.Models
{
    public class InvokerModel
    {
        public string DestinationMemberName { get; set; }
        public LambdaExpression? Invoker { get; set; }
        public string? SourceMemberName { get; set; }
        public LambdaExpression? Condition { get; set; }
        public TypeAdapterSettings? OvverideSettings { get; set; }
        public bool IsChildPath { get; set; }

        public InvokerModel? Next(ParameterExpression source, string destMemberName)
        {
            if (!DestinationMemberName.StartsWith(destMemberName + "."))
                return null;

            return new InvokerModel
            {
                DestinationMemberName = DestinationMemberName.Substring(destMemberName.Length + 1),
                Condition = IsChildPath || Condition == null
                    ? Condition
                    : Expression.Lambda(Condition.Apply(source), source),
                Invoker = IsChildPath
                    ? Invoker
                    : Expression.Lambda(GetInvokingExpression(source), source),
                SourceMemberName = SourceMemberName,
                IsChildPath = true,
            };
        }

        public Expression GetInvokingExpression(Expression exp, MapType mapType = MapType.Map, bool isExtraParam = false)
        {
            if (IsChildPath)
                return Invoker!.Body;
            return SourceMemberName != null
                ? ExpressionEx.PropertyOrFieldPath(exp, SourceMemberName)
                : isExtraParam ? Invoker!.ApplyExtraSources(mapType, exp) : Invoker!.Apply(mapType, exp);
        }

        public Expression? GetConditionExpression(Expression exp, MapType mapType = MapType.Map, bool isExtraParam = false)
        {
            return IsChildPath
                ? Condition?.Body
                : isExtraParam ? Condition?.ApplyExtraSources(mapType, exp) : Condition?.Apply(mapType, exp);
        }
    }

    public class InvokerModelApplyComparer : IEqualityComparer<InvokerModel>
    {
        public bool Equals(InvokerModel? x, InvokerModel? y)
        {
            if (x is null || y is null) return false;
            return string.Equals(x.DestinationMemberName, y.DestinationMemberName, System.StringComparison.InvariantCulture);
        }

        public int GetHashCode(InvokerModel obj)
        {
            return obj?.DestinationMemberName?.GetHashCode() ?? 0;
        }
    }
}