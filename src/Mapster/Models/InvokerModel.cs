using System.Linq.Expressions;

namespace Mapster.Models
{
    public class InvokerModel
    {
        public string DestinationMemberName;
        public LambdaExpression Invoker;
        public string SourceMemberName;
        public LambdaExpression Condition;

        public InvokerModel Next(string destMemberName)
        {
            if (!this.DestinationMemberName.StartsWith(destMemberName + "."))
                return null;

            return new InvokerModel
            {
                DestinationMemberName = this.DestinationMemberName.Substring(destMemberName.Length + 1),
                Condition = this.Condition,
                Invoker = this.Invoker,
                SourceMemberName = this.SourceMemberName,
            };
        }
    }
}