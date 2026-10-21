using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Mapster.Models
{
    public class MemberMapping
    {
        public Expression Getter => GetterLines.FirstOrDefault()?.Getter;
        public List<IGetterLineConsumer> GetterLines { get; } = new List<IGetterLineConsumer>();
        public IMemberModelEx DestinationMember;
        public Expression? RestoreDestinationMemberExp;
        public IgnoreDictionary.IgnoreItem Ignore;
        public List<InvokerModel> NextResolvers;
        public IgnoreDictionary NextIgnore;
        public ParameterExpression Source;
        public ParameterExpression? Destination;
        public ParameterExpression? Result;
        public bool UseDestinationValue;
        public TypeAdapterSettings? OverrideSettings;

        public bool HasSettings()
        {
            return NextResolvers.Count > 0 || NextIgnore.Count > 0;
        }

        public bool isNotFoundGetter => GetterLines.Count == 0;
        public bool IsMaybeReMapping => GetterLines.Any(x => x.IsCustomMap);
    }

    public interface IGetterLineConsumer
    {
        Expression? Condition { get;}
        Expression? Getter { get;}
        bool IsCustomMap { get;}
        Expression? NullPropagationChecker { get;}
        TypeAdapterSettings? OverrideSettings { get;}
        Expression Source { get;}
    }

    public class GetterLine : IGetterLineConsumer
    {
        public GetterLine(Expression source, Expression? getter, TypeAdapterSettings? overrideSettings, bool isCustomMap = false)
        {
            Source = source;
            Getter = getter;
            OverrideSettings = overrideSettings;
            IsCustomMap = isCustomMap;
        }

        public Expression Source { get; init; }
        public Expression? Getter { get; init; }
        public TypeAdapterSettings? OverrideSettings { get; init; }
        public Expression? Condition { get; set; }
        public Expression? NullPropagationChecker { get; set; }
        public bool IsCustomMap { get; init; }

    }
}