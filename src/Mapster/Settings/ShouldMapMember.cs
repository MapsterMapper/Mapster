using Mapster.Models;
using System;

namespace Mapster
{
    public static class ShouldMapMember
    {
        public static readonly Func<IMemberModel, MemberSide, bool?> AllowNonPublic = (model, _) => model.AccessModifier != AccessModifier.None;
        public static readonly Func<IMemberModel, MemberSide, bool?> AllowPublic = (model, _) => model.AccessModifier == AccessModifier.Public ? (bool?)true : null;
        public static readonly Func<IMemberModel, MemberSide, bool?> IgnoreAdaptIgnore = (model, side) =>
        {
            var ignoreAttr = model.GetCustomAttribute<AdaptIgnoreAttribute>();
            if (ignoreAttr == null)
                return null;
            return ignoreAttr.Side == null || ignoreAttr.Side == side ? (bool?) false : null;
        };
        public static readonly Func<IMemberModel, MemberSide, bool?> AllowAdaptMember = (model, _) => model.HasCustomAttribute(typeof(AdaptMemberAttribute)) ? (bool?)true : null;
    }
}
