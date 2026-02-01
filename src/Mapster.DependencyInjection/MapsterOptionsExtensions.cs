using System.Diagnostics.CodeAnalysis;

namespace Mapster.DependencyInjection;

/// <summary>
/// Helpers to apply <see cref="MapsterOptions"/> to <see cref="TypeAdapterConfig"/>.
/// </summary>
internal static class MapsterOptionsExtensions
{
	/// <summary>
	/// Applies the settings from <see cref="MapsterOptions"/> to the specified <see cref="TypeAdapterConfig"/>.
	/// </summary>
	/// <param name="options">The <see cref="MapsterOptions"/> instance containing the settings to apply.</param>
	/// <param name="config">The <see cref="TypeAdapterConfig"/> instance to which the settings will be applied.</param>
	/// <returns>The updated <see cref="TypeAdapterConfig"/> instance.</returns>
	public static TypeAdapterConfig ApplyTo(this MapsterOptions options, TypeAdapterConfig config) 
	{
		ArgumentNullException.ThrowIfNull(options);
		ArgumentNullException.ThrowIfNull(config);

		config.RequireDestinationMemberSource = options.RequireDestinationMemberSource;
		config.RequireExplicitMapping = options.RequireExplicitMapping;
		config.RequireExplicitMappingPrimitive = options.RequireExplicitMappingPrimitive;
		config.AllowImplicitDestinationInheritance = options.AllowImplicitDestinationInheritance;
		config.AllowImplicitSourceInheritance = options.AllowImplicitSourceInheritance;
		config.SelfContainedCodeGeneration = options.SelfContainedCodeGeneration;

		return config;
	}
}