using System;
using System.ComponentModel.DataAnnotations;
using Mapster;
using Newtonsoft.Json;

namespace Sample.CodeGen.Attributes
{
    public sealed class GenerateAddAttribute : AdaptFromAttribute
    {
        public GenerateAddAttribute() : base("[name]Add")
        {
            Initialize();
        }

        public GenerateAddAttribute(Type type) : base(type)
        {
            Initialize();
        }

        private void Initialize()
        {
            IgnoreAttributes = new[]
            {
                typeof(NoModifyAttribute),
                typeof(JsonIgnoreAttribute),
            };
            MapType = MapType.Map;
            ShallowCopyForSameType = true;
        }
    }
}
