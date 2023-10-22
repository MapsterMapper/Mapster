using System;

namespace Mapster
{
    [AttributeUsage(AttributeTargets.Class
                    | AttributeTargets.Struct
                    | AttributeTargets.Interface
                    | AttributeTargets.Property
                    | AttributeTargets.Field, AllowMultiple = true)]
    public class AdaptAsAttribute : Attribute
    {
        public AdaptDirectives AdaptDirective { get; set; }
        public AdaptAsAttribute(AdaptDirectives directive)
        {
            AdaptDirective = directive;
        }
    }
}
