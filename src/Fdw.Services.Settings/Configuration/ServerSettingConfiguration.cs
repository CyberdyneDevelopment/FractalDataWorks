using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Settings.Configuration;

/// <summary>
/// Database-backed configuration for server-level settings.
/// Generates the table <c>settings.ServerSetting</c>.
/// </summary>
/// <remarks>
/// Server settings are the base layer of the layered settings hierarchy:
/// Server (default) → Tenant (override) → Role (override).
/// Each setting has a DataType and optional MinValue/MaxValue for clamping overrides.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration( ServiceCategory = "Settings",
    ServiceType = "ServerSetting")]
public sealed partial class ServerSettingConfiguration : IGenericConfiguration
{
    /// <summary>
    /// Gets or sets the unique identifier for this setting.
    /// </summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>
    /// Gets or sets the setting name (unique key).
    /// </summary>
    public string SettingName { get; set; } = string.Empty;

    /// <inheritdoc />
    string IGenericConfiguration.Name
    {
        get => SettingName;
        set => SettingName = value;
    }

    /// <inheritdoc />
    string IGenericConfiguration.SectionName => "Settings:ServerSetting";

    /// <inheritdoc />
    string IGenericConfiguration.ServiceType => "ServerSetting";

    /// <inheritdoc />
    string? IGenericConfiguration.ServiceOptionType => null;

    /// <summary>
    /// Gets or sets the setting value as a string representation.
    /// </summary>
    public string SettingValue { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the data type of the setting value (e.g., "String", "Int32", "Boolean", "Decimal").
    /// </summary>
    public string DataType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional description of this setting.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the optional minimum allowed value for numeric settings.
    /// Used to clamp tenant and role overrides.
    /// </summary>
    public string? MinValue { get; set; }

    /// <summary>
    /// Gets or sets the optional maximum allowed value for numeric settings.
    /// Used to clamp tenant and role overrides.
    /// </summary>
    public string? MaxValue { get; set; }

    /// <summary>
    /// Gets or sets whether this setting is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

}
