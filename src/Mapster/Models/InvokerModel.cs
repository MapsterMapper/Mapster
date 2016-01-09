using System.Linq.Expressions;

namespace Mapster.Models
{
    public class InvokerModel
    {
        public string MemberName;

        public LambdaExpression Invoker;

        public LambdaExpression Condition;
    }
}