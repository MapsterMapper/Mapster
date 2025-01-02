using System;

namespace Mapster
{
    [AttributeUsage(AttributeTargets.Class
                    | AttributeTargets.Struct
                    | AttributeTargets.Property
                    | AttributeTargets.Field, AllowMultiple = true)]
    public class AdaptWithAttribute : Attribute
    {
        public AdaptDirectives AdaptDirective { get; set; }
        public AdaptWithAttribute(AdaptDirectives directive)
        {
            AdaptDirective = directive;
        }
    }
}
