using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Mapster.Models;

namespace Mapster.Adapters
{
    internal class ClassWithNonPublicMemberAdapter : ClassAdapter
    {
        protected override bool CanMap(Type sourceType, Type destinationType, MapType mapType)
        {
            if (sourceType == typeof(string) || sourceType == typeof(object))
                return false;

            if (destinationType.GetTypeInfo().IsEnum || destinationType.Namespace.StartsWith("System"))
                return false;

            return (destinationType.GetFieldsAndProperties(allowNoSetter: false).Any() ||
                    destinationType.GetFieldsAndProperties(allowNoSetter: false, isNonPublic: true).Any());
        }

        protected override bool CanInline(Expression source, Expression destination, CompileArgument arg)
        {
            if (!base.CanInline(source, destination, arg))
                return false;
            if (arg.MapType != MapType.Projection &&
                arg.Settings.IgnoreNullValues == true)
                return false;
            if (arg.DestinationType.GetFieldsAndProperties(isNonPublic: true).Any())
                return false;
            return true;
        }

        protected override ClassModel GetClassModel(Type destinationType)
        {
            return new ClassModel
            {
                Members = destinationType.GetFieldsAndProperties(allowNoSetter: false)
                    .Concat(destinationType.GetFieldsAndProperties(allowNoSetter: false, isNonPublic: true))
            };
        }
    }
}
