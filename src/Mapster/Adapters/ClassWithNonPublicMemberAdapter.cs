using System;
using System.Reflection;
using Mapster.Models;

namespace Mapster.Adapters
{
    internal class ClassWithNonPublicMemberAdapter : ClassAdapter
    {
        protected override ClassModel GetClassModel(Type destinationType)
        {
            return new ClassModel
            {
                Members = destinationType.GetFieldsAndProperties(allowNoSetter: false, accessorFlags: BindingFlags.Public | BindingFlags.NonPublic)
            };
        }
    }
}
