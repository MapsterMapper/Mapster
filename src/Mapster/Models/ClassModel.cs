using System.Collections.Generic;
using System.Reflection;

namespace Mapster.Models
{
    internal class ClassModel
    {
        public ConstructorInfo ConstructorInfo { get; set; }
        public IEnumerable<IMemberModel> Members { get; set; } 
    }
}
