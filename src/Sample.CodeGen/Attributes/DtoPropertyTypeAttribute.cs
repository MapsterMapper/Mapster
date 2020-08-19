using System;
using Mapster;

namespace Sample.CodeGen.Attributes
{
    public sealed class DtoPropertyTypeAttribute : PropertyTypeAttribute
    {
        public DtoPropertyTypeAttribute(Type type) : base(type)
        {
            ForAttributes = new[] {typeof(GenerateDtoAttribute)};
        }
    }
}
