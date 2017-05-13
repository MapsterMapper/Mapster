using Mapster.Models;
using System;

namespace Mapster
{
    public static class GetMemberName
    {
        public static Func<IMemberModel, string> AdaptMember = model => model.GetCustomAttribute<AdaptMemberAttribute>()?.Name;
    }
}
