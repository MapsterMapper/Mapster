using System;
using System.Linq.Expressions;
using Mapster.Utils;

namespace Mapster.Models
{
    public class PropertyModel
    {
        public Expression Getter;
        public Expression Setter;

        public byte ConvertType; //Primitive = 1, FlatteningGetMethod = 2, FlatteningDeep = 3, Adapter = 4, CustomResolve = 5;

        public string SetterPropertyName;
    }
}