using System.Linq.Expressions;

namespace Mapster.Models
{
    public class InvokerModel
    {
        public string DestinationMemberName;
        public LambdaExpression Invoker;
        public string SourceMemberName;
        public LambdaExpression Condition;
    }
}