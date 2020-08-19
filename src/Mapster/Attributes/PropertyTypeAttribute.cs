using System;

namespace Mapster
{
    [AttributeUsage(AttributeTargets.Class 
                    | AttributeTargets.Struct 
                    | AttributeTargets.Interface 
                    | AttributeTargets.Property 
                    | AttributeTargets.Field, AllowMultiple = true)]
    public class PropertyTypeAttribute : Attribute
    {
        public Type Type { get; }
        public Type[]? ForAttributes { get; set; }

        public PropertyTypeAttribute(Type type)
        {
            this.Type = type;
        }
    }
}
