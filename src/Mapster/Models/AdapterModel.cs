using System.Collections.Generic;

namespace Mapster.Models
{
    internal class AdapterModel
    {
        public static readonly AdapterModel Default = new AdapterModel
        {
            Properties = new List<PropertyModel>(),
            UnmappedProperties = new List<string>(),
        };

        public List<PropertyModel> Properties;
        public List<string> UnmappedProperties;
    }
}
