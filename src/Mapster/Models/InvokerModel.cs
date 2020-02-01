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
            if (!this.DestinationMemberName.StartsWith(destMemberName + "."))
                return null;

            return new InvokerModel
            {
                DestinationMemberName = this.DestinationMemberName.Substring(destMemberName.Length + 1),
                Condition = this.IsChildPath || this.Condition == null
                    ? this.Condition
                    : Expression.Lambda(this.Condition.Apply(source), source),
                Invoker = this.IsChildPath
                    ? this.Invoker
                    : Expression.Lambda(this.GetInvokingExpression(source), source),
                SourceMemberName = this.SourceMemberName,
                IsChildPath = true,
            };
        }

        public Expression GetInvokingExpression(Expression exp, MapType mapType = MapType.Map)
        {
            if (this.IsChildPath)
                return this.Invoker!.Body;
            return this.SourceMemberName != null
                ? ExpressionEx.PropertyOrFieldPath(exp, this.SourceMemberName)
                : this.Invoker!.Apply(mapType, exp);
        }

        public Expression? GetConditionExpression(Expression exp, MapType mapType = MapType.Map)
        {
            return this.IsChildPath
                ? this.Condition?.Body
                : this.Condition?.Apply(mapType, exp);
        }
    }
}