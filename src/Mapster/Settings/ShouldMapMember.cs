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
                : !(model.Info is FieldInfo) || model.GetCustomAttribute<CompilerGeneratedAttribute>() == null;
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
