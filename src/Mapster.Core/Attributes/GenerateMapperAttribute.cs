using System;

namespace Mapster
{
    [AttributeUsage(AttributeTargets.Class 
                    | AttributeTargets.Struct 
                    | AttributeTargets.Interface)]
    public class GenerateMapperAttribute : Attribute
    {
        public string Name { get; set; } = "[name]Mapper";
        public Type[]? ForAttributes { get; set; }
        public bool IsHelperClass { get; set; }
        public bool IsInternal { get; set; }
    }
}
