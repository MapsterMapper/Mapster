using System;

namespace Mapster
{
    [AttributeUsage(AttributeTargets.Field 
        | AttributeTargets.Parameter 
        | AttributeTargets.Property)]
    public class AdaptMemberAttribute : Attribute
    {
        public string? Name { get; set; }
        public MemberSide? Side { get; set; }

        public AdaptMemberAttribute() { }
        public AdaptMemberAttribute(string name)
        {
            this.Name = name;
        }
        public AdaptMemberAttribute(string name, MemberSide side)
        {
            this.Name = name;
            this.Side = side;
        }
    }
}
