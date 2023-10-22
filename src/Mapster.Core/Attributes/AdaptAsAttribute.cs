using System;

namespace Mapster.Attributes
{
    [AttributeUsage(AttributeTargets.Class
                    | AttributeTargets.Struct
                    | AttributeTargets.Interface
                    | AttributeTargets.Property
                    | AttributeTargets.Field, AllowMultiple = true)]
    public class AdaptAsAttribute : Attribute
    {
        public AdaptDirectives AdaptDirective { get; }
        public AdaptAsAttribute(AdaptDirectives directive)
        {
            this.AdaptDirective = directive;
        }
    }
}
