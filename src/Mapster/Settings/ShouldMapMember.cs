using Mapster.Models;
using System;

namespace Mapster
{
    public static class ShouldMapMember
    {
        public static Func<IMemberModel, bool?> AllowNonPublic = model => model.AccessModifier != AccessModifier.None;
        public static Func<IMemberModel, bool?> AllowPublic = model => model.AccessModifier == AccessModifier.Public ? (bool?)true : null;
        public static Func<IMemberModel, bool?> IgnoreAdaptIgnore = model => model.HasCustomAttribute(typeof(AdaptIgnoreAttribute)) ? (bool?)false : null;
        public static Func<IMemberModel, bool?> AllowAdaptMember = model => model.HasCustomAttribute(typeof(AdaptMemberAttribute)) ? (bool?)true : null;
    }
}
