using System;
using System.Linq.Expressions;

namespace Mapster.Models
{
    public class InvokerModel
    {
        public string MemberName;

        public Expression Invoker;

        public Expression Condition;
    }
}