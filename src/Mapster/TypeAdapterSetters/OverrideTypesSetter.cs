using Mapster.Utils;
using System;
using System.Linq.Expressions;

namespace Mapster
{
    [AdaptWith(AdaptDirectives.DestinationAsRecord)]
    public class OverrideTypesSetter : TypeAdapterSetter
    {
        internal protected OverrideTypesSettings _Settings { get => (OverrideTypesSettings)Settings; }

        public OverrideTypesSetter(TypeAdapterConfig config) : this (new OverrideTypesSettings (), config) { }
        public OverrideTypesSetter(TypeAdapterSettings settings, TypeAdapterConfig config) : base(settings, config) { }
    }

    public class OverrideTypesSetter<TSource, TDestination> : OverrideTypesSetter
    {
        public OverrideTypesSetter(TypeAdapterConfig config) : base(config)
        {
        }

        public OverrideTypesSetter(TypeAdapterSettings settings, TypeAdapterConfig config) : base(settings, config)
        {
        }

        public OverrideTypesSetter<TSource, TDestination> SkipAllSettings(bool value)
        {
            _Settings.SkipAllSettings = value;
            return this;
        }

        [Obsolete("This method will be removed in the release version." +
            "It is used for debugging and finding settings that cannot be overridden by existing settings setters.")]
        public OverrideTypesSetter<TSource, TDestination> SkipSettings(params Expression<Func<TypeAdapterSettings, object>>[] settings)
        {
            foreach (var member in settings)
            {
                _Settings.SkipSettings.Add(member.GetMemberPath()!);
            }

            return this;
        }

        public TypeAdapterSetter<TSource, TDestination> ReConfigurate()
        {
            return new TypeAdapterSetter<TSource, TDestination>(this.Settings,this.Config);
        }

        public OverrideTypesSetter<TSource, TDestination> SkipDestinationTransforms()
        {
            this.SkipSettings(x => x.DestinationTransforms);
            return this;
        }
    }

    
}
