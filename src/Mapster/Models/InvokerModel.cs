using System;
using System.Linq.Expressions;

namespace Mapster.Models
{
    internal class InvokerModel
    {
        public string MemberName;

        public Expression Invoker;

        public Expression Condition;
    }
}