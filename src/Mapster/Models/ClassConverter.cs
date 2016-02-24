using System.Collections.Generic;
using System.Reflection;

namespace Mapster.Models
{
    internal class ClassConverter
    {
        public ConstructorInfo ConstructorInfo { get; set; }
        public List<MemberConverter> Members { get; set; }
    }
}
