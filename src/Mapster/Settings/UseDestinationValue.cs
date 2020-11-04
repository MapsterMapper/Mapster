using System;
using Mapster.Models;

namespace Mapster
{
    public static class UseDestinationValue
    {
        public static readonly Func<IMemberModel, bool> Attribute = model => model.HasCustomAttribute<UseDestinationValueAttribute>();
    }
}
