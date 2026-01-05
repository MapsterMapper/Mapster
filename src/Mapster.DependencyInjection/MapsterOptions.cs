namespace Mapster.DependencyInjection;

/// <summary>
/// Options for configuring Mapster through DI or configuration binding.
/// </summary>
public sealed record MapsterOptions
{
    /// <summary>
    /// The default configuration section name used for options binding.
    /// </summary>
    public const string DefaultSectionName = "Mapster";

    /// <summary>
    /// Engine-level configuration switches applied to <see cref="TypeAdapterConfig"/>.
    /// </summary>
    public MapsterConfigOptions Config { get; init; } = new();

    /// <summary>
    /// Global default mapping settings applied to <see cref="TypeAdapterConfig.Default"/>.
    /// </summary>
    public MapsterSettingsOptions Settings { get; init; } = new();

    public void ApplyTo(TypeAdapterConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        var defaultConfig = TypeAdapterConfig.GlobalSettings;
        var defaultSettings = defaultConfig.Default.Settings;

        if (Config.RequireDestinationMemberSource != defaultConfig.RequireDestinationMemberSource)
        {
            config.RequireDestinationMemberSource = Config.RequireDestinationMemberSource;
        }

        if (Config.RequireExplicitMapping != defaultConfig.RequireExplicitMapping)
        {
            config.RequireExplicitMapping = Config.RequireExplicitMapping;
        }

        if (Config.RequireExplicitMappingPrimitive != defaultConfig.RequireExplicitMappingPrimitive)
        {
            config.RequireExplicitMappingPrimitive = Config.RequireExplicitMappingPrimitive;
        }

        if (Config.AllowImplicitDestinationInheritance != defaultConfig.AllowImplicitDestinationInheritance)
        {
            config.AllowImplicitDestinationInheritance = Config.AllowImplicitDestinationInheritance;
        }

        if (Config.AllowImplicitSourceInheritance != defaultConfig.AllowImplicitSourceInheritance)
        {
            config.AllowImplicitSourceInheritance = Config.AllowImplicitSourceInheritance;
        }

        if (Config.SelfContainedCodeGeneration != defaultConfig.SelfContainedCodeGeneration)
        {
            config.SelfContainedCodeGeneration = Config.SelfContainedCodeGeneration;
        }

        if (!string.IsNullOrWhiteSpace(Settings.NameMatchingStrategy))
        {
            config.Default.ApplyNameMatchingStrategy(Settings.NameMatchingStrategy);
        }

        if (Settings.MapToConstructor)
        {
            config.Default.MapToConstructor(Settings.MapToConstructor);
        }

        if (Settings.PreserveReference)
        {
            config.Default.PreserveReference(Settings.PreserveReference);
        }

        if (Settings.ShallowCopyForSameType)
        {
            config.Default.ShallowCopyForSameType(Settings.ShallowCopyForSameType);
        }

        if (Settings.IgnoreNullValues)
        {
            config.Default.IgnoreNullValues(Settings.IgnoreNullValues);
        }

        if (Settings.MapEnumByName != (defaultSettings.MapEnumByName ?? false))
        {
            config.Default.Settings.MapEnumByName = Settings.MapEnumByName;
        }

        if (Settings.IgnoreNonMapped)
        {
            config.Default.IgnoreNonMapped(Settings.IgnoreNonMapped);
        }

        if (Settings.AvoidInlineMapping != (defaultSettings.AvoidInlineMapping ?? false))
        {
            config.Default.AvoidInlineMapping(Settings.AvoidInlineMapping);
        }

        if (Settings.Unflattening != (defaultSettings.Unflattening ?? false))
        {
            config.Default.Unflattening(Settings.Unflattening);
        }

        if (Settings.SkipDestinationMemberCheck)
        {
            config.Default.Settings.SkipDestinationMemberCheck = Settings.SkipDestinationMemberCheck;
        }

        if (Settings.EnableNonPublicMembers)
        {
            config.Default.EnableNonPublicMembers(Settings.EnableNonPublicMembers);
        }

        if (Settings.MaxDepth > 0)
        {
            config.Default.MaxDepth(Settings.MaxDepth);
        }
    }
}

