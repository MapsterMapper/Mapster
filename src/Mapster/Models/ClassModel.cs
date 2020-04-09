using System.Collections.Generic;
using System.Reflection;

namespace Mapster.Models
{
    internal class ClassModel
    {
        public bool BreakOnUnmatched { get; set; }
        public ConstructorInfo? ConstructorInfo { get; set; }
        public IEnumerable<IMemberModelEx> Members { get; set; } 
    }
}
