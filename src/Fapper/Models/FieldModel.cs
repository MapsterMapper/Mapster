using Fpr.Utils;

namespace Fpr.Models
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