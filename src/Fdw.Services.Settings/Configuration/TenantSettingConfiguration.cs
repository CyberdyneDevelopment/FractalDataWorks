using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Settings.Configuration;

/// <summary>
/// Database-backed configuration for tenant-level setting overrides.
/// Generates the table <c>settings.TenantSetting</c>.
/// </summary>
/// <remarks>
/// Tenant settings override server-level defaults for a specific tenant.
/// Resolution order: Server (default) → Tenant (override) → Role (override).
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration( ServiceCategory = "Settings",
    ServiceType = "TenantSetting")]
public sealed partial class TenantSettingConfiguration : IGenericConfiguration, ITenantSettingImplementationConfiguration
{
    /// <summary>Gets or sets the domain this implementation belongs to.</summary>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Domain { get; set; } = string.Empty;

    /// <summary>Gets or sets the name, set by the domain provider from the domain row.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the unique identifier for this tenant setting.
    /// </summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>
    /// Gets or sets the tenant this setting override belongs to.
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Gets or sets the setting name (must match a ServerSetting.SettingName).
    /// </summary>
    public string SettingName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the overridden setting value for this tenant.
    /// </summary>
    public string SettingValue { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether this setting override is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

}
