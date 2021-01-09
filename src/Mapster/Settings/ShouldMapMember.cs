using Mapster.Models;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Mapster
{
    public static class ShouldMapMember
    {
        public static readonly Func<IMemberModel, MemberSide, bool?> AllowNonPublic = (model, _) =>
            (model.AccessModifier & AccessModifier.NonPublic) == 0
                ? (bool?) null
                : !(model.Info is FieldInfo) || !model.HasCustomAttribute<CompilerGeneratedAttribute>();
        public static readonly Func<IMemberModel, MemberSide, bool?> AllowPublic = (model, _) => model.AccessModifier == AccessModifier.Public ? (bool?)true : null;
        public static readonly Func<IMemberModel, MemberSide, bool?> IgnoreAdaptIgnore = (model, side) =>
        {
            var ignoreAttr = model.GetCustomAttributeFromData<AdaptIgnoreAttribute>();
            if (ignoreAttr == null)
                return null;
            return ignoreAttr.Side == null || ignoreAttr.Side == side ? (bool?) false : null;
        };
        public static readonly Func<IMemberModel, MemberSide, bool?> AllowAdaptMember = (model, side) =>
        {
            var memberAttr = model.GetCustomAttributeFromData<AdaptMemberAttribute>();
            if (memberAttr == null)
                return null;
            return memberAttr.Side == null || memberAttr.Side == side ? (bool?) true : null;
        };
    }
}
