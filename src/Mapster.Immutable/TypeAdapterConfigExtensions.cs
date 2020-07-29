using Mapster.Immutable;

namespace Mapster
{
    public static class TypeAdapterConfigExtensions
    {
        public static void EnableImmutableMapping(this TypeAdapterConfig config)
        {
            config.Rules.Add(new ImmutableAdapter().CreateRule());
        }
    }
}
