using System;
using System.ComponentModel.DataAnnotations;
using Mapster;
using Newtonsoft.Json;

namespace Sample.CodeGen.Attributes
{
    public sealed class GenerateUpdateAttribute : AdaptFromAttribute
    {
        public GenerateUpdateAttribute() : base("[name]Update")
        {
            Initialize();
        }

        public GenerateUpdateAttribute(Type type) : base(type)
        {
            Initialize();
        }

        private void Initialize()
        {
            IgnoreAttributes = new[]
            {
                typeof(KeyAttribute), 
                typeof(NoModifyAttribute),
                typeof(JsonIgnoreAttribute),
            };
            MapType = MapType.MapToTarget;
            ShallowCopyForSameType = true;
        }
    }
}
