using System;

namespace Mapster
{
    [AttributeUsage(AttributeTargets.Field 
        | AttributeTargets.Parameter 
        | AttributeTargets.Property)]
    public class AdaptMemberAttribute : Attribute
    {
        public string? Name { get; set; }

        public AdaptMemberAttribute() { }
        public AdaptMemberAttribute(string name)
        {
            this.Name = name;
        }
    }
}
