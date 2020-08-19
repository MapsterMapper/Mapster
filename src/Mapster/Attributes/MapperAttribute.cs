using System;

namespace Mapster
{
    [AttributeUsage(AttributeTargets.Interface)]
    public class MapperAttribute : Attribute
    {
        public string? Name { get; set; }
        public bool IsInternal { get; set; }
    }
}