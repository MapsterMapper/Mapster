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

}

