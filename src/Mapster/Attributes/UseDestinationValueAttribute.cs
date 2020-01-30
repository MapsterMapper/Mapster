using System;

namespace Mapster
{
    [AttributeUsage(AttributeTargets.Field
                    | AttributeTargets.Property)]
    public class UseDestinationValueAttribute : Attribute
    {
    }
}
