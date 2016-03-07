using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Mapster.Models
{
    internal class MemberMapping
    {
        public Expression Getter;
        public Expression Setter;

        public byte ConvertType; //Primitive = 1, FlatteningGetMethod = 2, FlatteningDeep = 3, Adapter = 4, CustomResolve = 5;

        public object SetterInfo;
    }
}