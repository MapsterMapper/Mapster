using System.Linq.Expressions;
using Mapster.Utils;

namespace Mapster.Models
{
    public class InvokerModel
    {
        public string DestinationMemberName { get; set; }
        public LambdaExpression? Invoker { get; set; }
        public string? SourceMemberName { get; set; }
        public LambdaExpression? Condition { get; set; }
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

        public Expression GetInvokingExpression(Expression exp, MapType mapType = MapType.Map)
        {
            if (IsChildPath)
                return Invoker!.Body;
            return SourceMemberName != null
                ? ExpressionEx.PropertyOrFieldPath(exp, SourceMemberName)
                : Invoker!.Apply(mapType, exp);
        }

        public Expression? GetConditionExpression(Expression exp, MapType mapType = MapType.Map)
        {
            return IsChildPath
                ? Condition?.Body
                : Condition?.Apply(mapType, exp);
        }
    }
}