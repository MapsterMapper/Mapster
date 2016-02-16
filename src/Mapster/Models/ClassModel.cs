using System.Collections.Generic;
using System.Reflection;

namespace Mapster.Models
{
    internal class ClassModel
    {
        public ConstructorInfo ConstructorInfo { get; set; }
        public List<IMemberModel> Members { get; set; } 
    }
}
