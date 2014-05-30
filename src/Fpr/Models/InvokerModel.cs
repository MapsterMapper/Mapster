using System;

namespace Fpr.Models
{
    public class InvokerModel<TSource>
    {
        public string MemberName;

        public Func<TSource, object> Invoker;

        public Func<TSource, bool> Condition;
    }
}