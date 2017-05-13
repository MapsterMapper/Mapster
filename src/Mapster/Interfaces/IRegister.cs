namespace Mapster
{
    /// <summary>
    /// Implement to allow mappings to be found when scanning assemblies.
    /// Place mappings in the Register method.
    /// Call TypeAdapterConfig.ScanAssemblies to perform scanning <see cref="TypeAdapterConfig"/>
    /// </summary>
    public interface IRegister
    {
        void Register(TypeAdapterConfig config);
    }
}