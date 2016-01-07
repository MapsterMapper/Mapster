using System.Linq.Expressions;

namespace Mapster.Models
{
    internal class PropertyModel
    {
        public Expression Getter;
        public Expression Setter;

        public byte ConvertType; //Primitive = 1, FlatteningGetMethod = 2, FlatteningDeep = 3, Adapter = 4, CustomResolve = 5;

        public string SetterPropertyName;
    }
}