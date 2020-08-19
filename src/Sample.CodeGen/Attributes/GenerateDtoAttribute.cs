using System;
using Mapster;
using Newtonsoft.Json;

namespace Sample.CodeGen.Attributes
{
    public sealed class GenerateDtoAttribute : AdaptToAttribute
    {
        public GenerateDtoAttribute() : base("[name]Dto")
        {
            Initialize();
        }

        public GenerateDtoAttribute(Type type) : base(type)
        {
            Initialize();
        }

        private void Initialize()
        {
            IgnoreAttributes = new[] {typeof(JsonIgnoreAttribute)};
            ShallowCopyForSameType = true;
        }
    }
}
