using Mapster.Models;
using System;

namespace Mapster
{
    public static class ShouldMapMember
    {
        public static Func<IMemberModel, MemberSide, bool?> AllowNonPublic = (model, _) => model.AccessModifier != AccessModifier.None;
        public static Func<IMemberModel, MemberSide, bool?> AllowPublic = (model, _) => model.AccessModifier == AccessModifier.Public ? (bool?)true : null;
        public static Func<IMemberModel, MemberSide, bool?> IgnoreAdaptIgnore = (model, _) => model.HasCustomAttribute(typeof(AdaptIgnoreAttribute)) ? (bool?)false : null;
        public static Func<IMemberModel, MemberSide, bool?> AllowAdaptMember = (model, _) => model.HasCustomAttribute(typeof(AdaptMemberAttribute)) ? (bool?)true : null;
    }
}
