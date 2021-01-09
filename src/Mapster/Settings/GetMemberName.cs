using Mapster.Models;
using System;

namespace Mapster
{
    public static class GetMemberName
    {
        public static readonly Func<IMemberModel, MemberSide, string?> AdaptMember = (model, side) =>
        {
            var memberAttr = model.GetCustomAttributeFromData<AdaptMemberAttribute>();
            if (memberAttr == null)
                return null;
            return memberAttr.Side == null || memberAttr.Side == side ? memberAttr.Name : null;
        };
    }
}
