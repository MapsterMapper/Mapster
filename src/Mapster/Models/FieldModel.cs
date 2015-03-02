using Mapster.Utils;

namespace Mapster.Models
{
    /// <summary>
    /// Getter : FieldInfo.GetValue
    /// Setter : FieldInfo.SetValue
    /// </summary>
    public class FieldModel
    {
        public FastInvokeHandler Getter;
        public FastInvokeHandler Setter;
    }
}