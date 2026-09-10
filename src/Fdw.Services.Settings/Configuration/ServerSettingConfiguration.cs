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
public sealed partial class ServerSettingConfiguration : IGenericConfiguration, IServerSettingImplementationConfiguration
{
    /// <summary>Gets or sets the domain this implementation belongs to.</summary>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Domain { get; set; } = string.Empty;

    /// <summary>Gets or sets the name, set by the domain provider from the domain row.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the unique identifier for this setting.
    /// </summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>
    /// Gets or sets the setting name (unique key).
    /// </summary>
    public string SettingName { get; set; } = string.Empty;

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
