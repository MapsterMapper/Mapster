namespace Mapster.DependencyInjection;

/// <summary>
/// Global default mapping settings applied to <see cref="TypeAdapterConfig.Default"/>.
/// </summary>
public sealed record MapsterSettingsOptions
{
    /// <summary>
    /// Name matching strategy (Exact, Flexible, IgnoreCase, ToCamelCase, FromCamelCase).
    /// </summary>
    public string? NameMatchingStrategy { get; init; }

    /// <summary>
    /// Enable mapping to constructors when available.
    /// </summary>
    public bool MapToConstructor { get; init; }

    /// <summary>
    /// Preserve reference cycles when mapping.
    /// </summary>
    public bool PreserveReference { get; init; }

    /// <summary>
    /// Use shallow copy optimization when source and destination types are the same.
    /// </summary>
    public bool ShallowCopyForSameType { get; init; }

    /// <summary>
    /// Ignore null source values.
    /// </summary>
    public bool IgnoreNullValues { get; init; }

    /// <summary>
    /// Map enums by name instead of numeric value.
    /// </summary>
    public bool MapEnumByName { get; init; }

    /// <summary>
    /// Ignore members not mapped explicitly.
    /// </summary>
    public bool IgnoreNonMapped { get; init; }

    /// <summary>
    /// Avoid inline mapping when generating code.
    /// </summary>
    public bool AvoidInlineMapping { get; init; }

    /// <summary>
    /// Enable unflattening (map nested source to flat destination).
    /// </summary>
    public bool Unflattening { get; init; }

    /// <summary>
    /// Skip destination member checks when mapping.
    /// </summary>
    public bool SkipDestinationMemberCheck { get; init; }

    /// <summary>
    /// Allow mapping to non-public members.
    /// </summary>
    public bool EnableNonPublicMembers { get; init; }

    /// <summary>
    /// Maximum mapping depth (0 = unlimited).
    /// </summary>
    public int MaxDepth { get; init; }
}
