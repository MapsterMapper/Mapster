using System.Linq.Expressions;

namespace Mapster.Models
{
    public record ExtraSourceModel(object Src, OverrideTypesSettings? Settings = null)
    {
        public static explicit operator ExtraSourceModel(Expression src) => new ExtraSourceModel(src);
        public static explicit operator ExtraSourceModel(string src) => new ExtraSourceModel(src);
    }
}
