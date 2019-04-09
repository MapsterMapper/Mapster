using System.Linq.Expressions;
using Mapster.Utils;

namespace Mapster.Models
{
    public class InvokerModel
    {
        public string DestinationMemberName;
        public LambdaExpression Invoker;
        public string SourceMemberName;
        public LambdaExpression Condition;

        public InvokerModel Next(ParameterExpression source, string destMemberName)
        {
            if (!this.DestinationMemberName.StartsWith(destMemberName + "."))
                return null;

            return new InvokerModel
            {
                DestinationMemberName = this.DestinationMemberName.Substring(destMemberName.Length + 1),
                Condition = this.Condition,
                Invoker = this.Invoker ?? Expression.Lambda(ExpressionEx.PropertyOrField(source, this.SourceMemberName), source),
                SourceMemberName = this.SourceMemberName,
            };
        }
    }
}