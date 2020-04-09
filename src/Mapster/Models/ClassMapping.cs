using System.Collections.Generic;
using System.Reflection;

namespace Mapster.Models
{
    internal class ClassMapping
    {
        public ConstructorInfo? ConstructorInfo { get; set; }
        public List<MemberMapping> Members { get; set; }
    }
}
