using System.Collections.Generic;

namespace Mapster
{
    public interface ISettingStore
    {
        Dictionary<string, object> ObjectStore { get; set; }
        Dictionary<string, bool?> BooleanStore { get; set; }
    }
}