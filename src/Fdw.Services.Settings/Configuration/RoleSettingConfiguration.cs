using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Settings.Configuration;

/// <summary>
/// Database-backed configuration for role-level setting overrides.
/// Generates the table <c>settings.RoleSetting</c>.
/// </summary>
/// <remarks>
/// Role settings override tenant-level values for a specific role within a tenant.
/// Resolution order: Server (default) → Tenant (override) → Role (override).
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration( ServiceCategory = "Settings",
    ServiceType = "RoleSetting")]
public sealed partial class RoleSettingConfiguration : IGenericConfiguration
{
    /// <summary>
    /// Gets or sets the unique identifier for this role setting.
    /// </summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>
    /// Gets or sets the tenant this role setting belongs to.
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Gets or sets the role name this setting override applies to.
    /// </summary>
    public string RoleName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the setting name (must match a ServerSetting.SettingName).
    /// </summary>
    public string SettingName { get; set; } = string.Empty;

    /// <inheritdoc />
    string IGenericConfiguration.Name
    {
        get => SettingName;
        set => SettingName = value;
    }

    /// <inheritdoc />
    string IGenericConfiguration.SectionName => "Settings:RoleSetting";

    /// <inheritdoc />
    string IGenericConfiguration.ServiceType => "RoleSetting";

    /// <inheritdoc />
    string? IGenericConfiguration.ServiceOptionType => null;

    /// <summary>
    /// Gets or sets the overridden setting value for this role.
    /// </summary>
    public string SettingValue { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether this setting override is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

}
