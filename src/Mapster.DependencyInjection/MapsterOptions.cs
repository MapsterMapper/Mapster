namespace Mapster.DependencyInjection;

/// <summary>
/// Options for configuring Mapster through DI or configuration binding.
/// </summary>
public sealed record MapsterOptions
{
    /// <summary>
    /// The default configuration section name used for options binding.
    /// </summary>
    public const string DefaultName = "Mapster";

	/// <summary>
	/// Require that every destination member has a source.
	/// </summary>
	public bool RequireDestinationMemberSource { get; set; }

	/// <summary>
	/// Require explicit mapping (disable implicit mapping).
	/// </summary>
	public bool RequireExplicitMapping { get; set; }

	/// <summary>
	/// Require explicit mapping even for primitive types.
	/// </summary>
	public bool RequireExplicitMappingPrimitive { get; set; }

	/// <summary>
	/// Allow implicit inheritance on destination types.
	/// </summary>
	public bool AllowImplicitDestinationInheritance { get; set; }

	/// <summary>
	/// Allow implicit inheritance on source types.
	/// </summary>
	public bool AllowImplicitSourceInheritance { get; set; } = true;

	/// <summary>
	/// Generate self-contained code (no shared helpers) for source generation.
	/// </summary>
	public bool SelfContainedCodeGeneration { get; set; }

	/// <summary>
	/// Name matching strategy (Exact, Flexible, IgnoreCase, ToCamelCase, FromCamelCase).
	/// </summary>
	public string? NameMatchingStrategy { get; set; }

	/// <summary>
	/// Enable mapping to constructors when available.
	/// </summary>
	public bool MapToConstructor { get; set; }

	/// <summary>
	/// Preserve reference cycles when mapping.
	/// </summary>
	public bool PreserveReference { get; set; }

	/// <summary>
	/// Use shallow copy optimization when source and destination types are the same.
	/// </summary>
	public bool ShallowCopyForSameType { get; set; }

	/// <summary>
	/// Ignore null source values.
	/// </summary>
	public bool IgnoreNullValues { get; set; }

	/// <summary>
	/// Map enums by name instead of numeric value.
	/// </summary>
	public bool MapEnumByName { get; set; }

	/// <summary>
	/// Ignore members not mapped explicitly.
	/// </summary>
	public bool IgnoreNonMapped { get; set; }

	/// <summary>
	/// Avoid inline mapping when generating code.
	/// </summary>
	public bool AvoidInlineMapping { get; set; }

	/// <summary>
	/// Enable unflattening (map nested source to flat destination).
	/// </summary>
	public bool Unflattening { get; set; }

	/// <summary>
	/// Skip destination member checks when mapping.
	/// </summary>
	public bool SkipDestinationMemberCheck { get; set; }

	/// <summary>
	/// Allow mapping to non-public members.
	/// </summary>
	public bool EnableNonPublicMembers { get; set; }

	/// <summary>
	/// Maximum mapping depth (0 = unlimited).
	/// </summary>
	public int MaxDepth { get; set; }
}

