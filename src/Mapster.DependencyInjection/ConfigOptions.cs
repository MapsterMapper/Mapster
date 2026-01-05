namespace Mapster.DependencyInjection;

/// <summary>
/// Engine-level configuration switches applied to <see cref="TypeAdapterConfig"/>.
/// </summary>
public sealed record MapsterConfigOptions
{
    /// <summary>
    /// Require that every destination member has a source.
    /// </summary>
    public bool RequireDestinationMemberSource { get; init; }

    /// <summary>
    /// Require explicit mapping (disable implicit mapping).
    /// </summary>
    public bool RequireExplicitMapping { get; init; }

    /// <summary>
    /// Require explicit mapping even for primitive types.
    /// </summary>
    public bool RequireExplicitMappingPrimitive { get; init; }

    /// <summary>
    /// Allow implicit inheritance on destination types.
    /// </summary>
    public bool AllowImplicitDestinationInheritance { get; init; }

    /// <summary>
    /// Allow implicit inheritance on source types.
    /// </summary>
    public bool AllowImplicitSourceInheritance { get; init; } = true;

    /// <summary>
    /// Generate self-contained code (no shared helpers) for source generation.
    /// </summary>
    public bool SelfContainedCodeGeneration { get; init; }
}
