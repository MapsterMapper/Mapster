using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapster
{
    [AttributeUsage(AttributeTargets.Field
        | AttributeTargets.Property)]
    public class AdaptIgnoreAttribute: Attribute { }
}
