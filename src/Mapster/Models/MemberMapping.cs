using System.Linq.Expressions;

namespace Mapster.Models
{
    internal class MemberMapping
    {
        public Expression Getter;
        public Expression Setter;

        public object SetterInfo;
    }
}