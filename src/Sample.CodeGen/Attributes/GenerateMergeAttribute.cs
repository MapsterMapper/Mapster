using System;
using System.ComponentModel.DataAnnotations;
using Mapster;
using Newtonsoft.Json;

namespace Sample.CodeGen.Attributes
{
    public sealed class GenerateMergeAttribute : AdaptFromAttribute
    {
        public GenerateMergeAttribute() : base("[name]Merge")
        {
            Initialize();
        }

        public GenerateMergeAttribute(Type type) : base(type)
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
            IgnoreNullValues = true;
        }
    }
}
