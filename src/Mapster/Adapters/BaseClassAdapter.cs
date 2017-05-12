using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Mapster.Models;

namespace Mapster.Adapters
{
    internal abstract class BaseClassAdapter : BaseAdapter
    {
        protected abstract ClassModel GetClassModel(Type destinationType);

        #region Build the Adapter Model

        protected ClassMapping CreateClassConverter(Expression source, Expression destination, CompileArgument arg)
        {
            var classModel = GetClassModel(arg.DestinationType);
            var destinationMembers = classModel.Members;

            var unmappedDestinationMembers = new List<string>();

            var properties = new List<MemberMapping>();

            foreach (var destinationMember in destinationMembers)
            {
                if (ProcessIgnores(arg.Settings, destinationMember, out var setterCondition))
                    continue;

                var member = destinationMember;
                var getter = arg.Settings.ValueAccessingStrategies
                    .Select(fn => fn(source, member, arg))
                    .FirstOrDefault(result => result != null);

                if (getter != null)
                {
                    var propertyModel = new MemberMapping
                    {
                        Getter = getter,
                        Setter = destinationMember.GetExpression(destination),
                        SetterInfo = destinationMember.Info,
                        SetterCondition = setterCondition,
                    };
                    properties.Add(propertyModel);
                }
                else if (classModel.ConstructorInfo != null)
                {
                    var propertyModel = new MemberMapping
                    {
                        Getter = null,
                        Setter = destinationMember.GetExpression(destination),
                        SetterInfo = destinationMember.Info,
                        SetterCondition = setterCondition,
                    };
                    properties.Add(propertyModel);
                }
                else if (destinationMember.SetterModifier != AccessModifier.None)
                {
                    unmappedDestinationMembers.Add(destinationMember.Name);
                }
            }

            if (arg.Context.Config.RequireDestinationMemberSource && unmappedDestinationMembers.Count > 0)
            {
                throw new InvalidOperationException($"The following members of destination class {arg.DestinationType} do not have a corresponding source member mapped or ignored:{string.Join(",", unmappedDestinationMembers)}");
            }

            return new ClassMapping
            {
                ConstructorInfo = classModel.ConstructorInfo,
                Members = properties,
            };
        }

        private static bool ProcessIgnores(
            TypeAdapterSettings config,
            IMemberModel destinationMember,
            out LambdaExpression condition)
        {
            condition = null;
            if (!destinationMember.ShouldMapMember(config.ShouldMapMember))
                return true;

            return config.IgnoreIfs.TryGetValue(destinationMember.Name, out condition)
                   && condition == null;
        }

        #endregion
    }
}
