using Mapster.Models;
using System;

namespace Mapster
{
    public static class GetMemberName
    {
        public static readonly Func<IMemberModel, string?> AdaptMember = model => model.GetCustomAttributeFromData<AdaptMemberAttribute>()?.Name;
    }
}
