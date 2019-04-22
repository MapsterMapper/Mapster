using System.Linq.Expressions;
using Mapster.Utils;

namespace Mapster.Models
{
    public class InvokerModel
    {
        public string DestinationMemberName { get; set; }
        public LambdaExpression Invoker { get; set; }
        public string SourceMemberName { get; set; }
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
                    : Expression.Lambda(this.Invoker?.Apply(source) ?? ExpressionEx.PropertyOrField(source, this.SourceMemberName), source),
                SourceMemberName = this.SourceMemberName,
                IsChildPath = true,
            };
        }
    }
}